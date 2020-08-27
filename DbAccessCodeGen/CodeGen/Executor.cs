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

        public Task Execute()
        {
            Channel<DBObjectName> toGetMetadata= Channel.CreateBounded<DBObjectName>(3);
            Channel<SPMetadata> CodeGenChannel = Channel.CreateBounded<SPMetadata>(20);
            Channel<(string name, string template)> methods = Channel.CreateUnbounded<(string, string)>();
            Task metadataTask = StartGetMetadata(toGetMetadata);

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(120 * 1000);
            

            Task allMetaDataTasks = Task.WhenAll(Enumerable.Range(1, 3).Select(s => ExecuteGetMetadataForSP(toGetMetadata.Reader, CodeGenChannel.Writer)))
                .ContinueWith(t=>
                {
                    CodeGenChannel.Writer.Complete();
                }, cts.Token);
            

            Task codeGenTasks = Task.WhenAll( Enumerable.Range(1, 2).Select(s => ExecuteCodeGen(CodeGenChannel.Reader, methods.Writer)))
                .ContinueWith(t=>
                {
                    methods.Writer.Complete();
                }, cts.Token);
            Task serviceGentask = codeGenHandler.WriteServiceClass(methods.Reader);
            return Task.WhenAll(metadataTask, allMetaDataTasks, codeGenTasks, serviceGentask);
        }

        protected async Task StartGetMetadata(ChannelWriter<DBObjectName> channelWriter)
        {
            foreach (var sp in settings.Procedures)
            {
                await channelWriter.WriteAsync(sp);
            }
            channelWriter.Complete();
        }

        protected async Task ExecuteGetMetadataForSP(ChannelReader<DBObjectName> metadataReader, ChannelWriter<SPMetadata> toWriteTo)
        {
            List<ValueTask> writeTasks = new List<ValueTask>();
            await foreach (var sp in metadataReader.ReadAllAsync())
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var con = scope.ServiceProvider.GetRequiredService<DbConnection>();
                    var spprm = this.sPParametersProvider.GetSPParameters(sp, con);
                    var result = this.sqlHelper.GetSPResultSet(sp, true);
                    writeTasks.Add(toWriteTo.WriteAsync(new SPMetadata(name: sp, parameters: spprm, 
                           fields: result, 
                           resultType: namingHandler.GetResultTypeName(sp),
                           parameterTypeName: namingHandler.GetParameterTypeName(sp),
                           methodName: namingHandler.GetServiceClassMethodName(sp)
                           )));
                }
            }
            await Task.WhenAll(writeTasks.Select(v => v.AsTask()));
        }

        public async Task ExecuteCodeGen(ChannelReader<SPMetadata> channelReader, ChannelWriter<(string name, string template)> methods)
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
