using DbAccessCodeGen.Configuration;
using DbAccessCodeGen.Objects;
using Kull.Data;
using Kull.DatabaseMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DbAccessCodeGen.CodeGen
{
    public partial class Executor
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Settings settings;
        private readonly ILogger logger;
        private readonly SPParametersProvider sPParametersProvider;
        private readonly SqlHelper sqlHelper;
        private readonly CodeGenHandler codeGenHandler;
        private readonly NamingHandler namingHandler;
        ConcurrentDictionary<DBObjectName, SqlFieldDescription[]> userDefinedTypes = new ConcurrentDictionary<DBObjectName, SqlFieldDescription[]>();

        public Executor(IServiceProvider serviceProvider, Configuration.Settings settings, ILogger<Executor> logger,
            SPParametersProvider sPParametersProvider,
            SqlHelper sqlHelper,
            CodeGenHandler codeGenHandler,
            NamingHandler namingHandler)
        {
            this.serviceProvider = serviceProvider;
            this.settings = settings;
            this.logger = logger;
            this.sPParametersProvider = sPParametersProvider;
            this.sqlHelper = sqlHelper;
            this.codeGenHandler = codeGenHandler;
            this.namingHandler = namingHandler;
        }

        public async Task Execute()
        {
            Channel<DBOperationSetting> toGetMetadata = Channel.CreateBounded<DBOperationSetting>(3);
            Channel<DbOperationMetadata> CodeGenChannel = Channel.CreateBounded<DbOperationMetadata>(20);
            Channel<(string name, string template)> methods = Channel.CreateUnbounded<(string, string)>();
            Task metadataTask = StartGetMetadata(toGetMetadata);

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(120 * 1000);


            Task allMetaDataTasks = Task.WhenAll(Enumerable.Range(1, 3).Select(s => ExecuteGetMetadataForSP(toGetMetadata.Reader, CodeGenChannel.Writer)))
                .ContinueWith(t =>
                {
                    CodeGenChannel.Writer.Complete();
                }, cts.Token);


            Task codeGenTasks = Task.WhenAll(Enumerable.Range(1, 2).Select(s => ExecuteCodeGen(CodeGenChannel.Reader, methods.Writer)))
                .ContinueWith(t =>
                {
                    methods.Writer.Complete();
                }, cts.Token);
            Task serviceGentask = codeGenHandler.WriteServiceClass(methods.Reader);
            await metadataTask;
            await allMetaDataTasks;
            await codeGenTasks;
            await serviceGentask;
        }

        protected async Task StartGetMetadata(ChannelWriter<DBOperationSetting> channelWriter)
        {
            List<(DBObjectName objectName, DBObjectType type)> allSps = new();
            var con = serviceProvider.GetRequiredService<DbConnection>();
            await con.OpenAsync();
            try
            {
                using (var rdr = await con.CreateCommand("SELECT SCHEMA_NAME(p.schema_id), p.name, 'StoredProcedure' AS Type FROM sys.procedures p" +
                    "   UNION ALL " +
                    " SELECT TABLE_SCHEMA, TABLE_NAME, 'Table' AS Type FROM INFORMATION_SCHEMA.TABLES ", System.Data.CommandType.Text)
                    .ExecuteReaderAsync())
                {
                    while (rdr.Read())
                    {
                        allSps.Add((new DBObjectName(rdr.GetString(0), rdr.GetString(1)), Enum.Parse<DBObjectType>(rdr.GetString(2), true)));
                    }
                }
                foreach (var sp in settings.DBOperations)
                {
                    if (allSps.Contains((sp.DBObjectName, sp.DBObjectType)))
                    {
                        await channelWriter.WriteAsync(sp);
                    }
                    else
                    {
                        logger.LogError($"Cannot find {sp.DBObjectType} {sp.DBObjectName}. Ignore it");
                    }
                }
            }
            finally
            {
                con.Close();
                channelWriter.Complete();
            }
        }

        protected async Task ExecuteGetMetadataForSP(ChannelReader<DBOperationSetting> metadataReader, ChannelWriter<DbOperationMetadata> toWriteTo)
        {
            List<ValueTask> writeTasks = new List<ValueTask>();
            await foreach (var sp in metadataReader.ReadAllAsync())
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var con = scope.ServiceProvider.GetRequiredService<DbConnection>();
                    try
                    {
                        var spprm = sp.DBObjectType == DBObjectType.StoredProcedure ?  await this.sPParametersProvider.GetSPParameters(sp.DBObjectName, con): 
                                Array.Empty<SPParameter>();
                        var ignoreParameters = sp.IgnoreParameters ?? settings.IgnoreParameters;
                        var spPrmNames = spprm.Select(s => s.SqlName.StartsWith("@") ? s.SqlName.Substring(1) : s.SqlName).ToArray();
                        var replaceParaemeters = (sp.ReplaceParameters ?? settings.ReplaceParameters)
                                .Where(p => spPrmNames.Contains(p.Key)).ToDictionary(k => k.Key, k => k.Value);
                        var toUsePrm = spprm
                                .Where(p => !ignoreParameters.Contains(p.SqlName.StartsWith("@") ? p.SqlName.Substring(1) : p.SqlName, StringComparer.OrdinalIgnoreCase))
                                .Where(p => !replaceParaemeters.ContainsKey(p.SqlName.StartsWith("@") ? p.SqlName.Substring(1) : p.SqlName))
                                .ToArray();
                        try
                        {
                            var result = sp.ExecuteOnly ? Array.Empty<SqlFieldDescription>():
                                sp.DBObjectType == DBObjectType.StoredProcedure ?
                                await this.sqlHelper.GetSPResultSet(con, sp.DBObjectName, settings.PersistResultPath, sp.ExecuteParameters):
                                await this.sqlHelper.GetTableOrViewFields(con, sp.DBObjectName);
                            writeTasks.Add(toWriteTo.WriteAsync(new DbOperationMetadata(name: sp.DBObjectName,
                                dBObjectType: sp.DBObjectType,    
                                parameters: toUsePrm,
                                    replaceParameters: replaceParaemeters,
                                   fields: result,
                                   resultType: namingHandler.GetResultTypeName(sp.DBObjectName, sp.DBObjectType),
                                   parameterTypeName: namingHandler.GetParameterTypeName(sp.DBObjectName),
                                   methodName: namingHandler.GetServiceClassMethodName(sp.DBObjectName, sp.DBObjectType),
                                   setting: sp
                                   )));
                        }
                        catch (Exception err)
                        {
                            logger.LogError("Could not get fields. \r\n{0}", err);
                            writeTasks.Add(toWriteTo.WriteAsync(new DbOperationMetadata(name: sp.DBObjectName,
                                dBObjectType: sp.DBObjectType,
                                parameters: toUsePrm,
                                  fields: new SqlFieldDescription[] { },
                                  replaceParameters: replaceParaemeters,
                                  resultType: namingHandler.GetResultTypeName(sp.DBObjectName, sp.DBObjectType),
                                  parameterTypeName: namingHandler.GetParameterTypeName(sp.DBObjectName),
                                  methodName: namingHandler.GetServiceClassMethodName(sp.DBObjectName, sp.DBObjectType),
                                  setting: sp
                                  )));
                        }
                    }
                    catch (Exception err)
                    {
                        logger.LogError("Could not get parameters. \r\n{0}", err);
                    }
                }
            }
            await Task.WhenAll(writeTasks.Select(v => v.AsTask()));
        }

        public async Task ExecuteCodeGen(ChannelReader<DbOperationMetadata> channelReader, ChannelWriter<(string name, string template)> methods)
        {
            await foreach (var codeGenPrm in channelReader.ReadAllAsync())
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    await codeGenHandler.ExecuteCodeGen(codeGenPrm, methods);
                }
            }
        }
    }
}
