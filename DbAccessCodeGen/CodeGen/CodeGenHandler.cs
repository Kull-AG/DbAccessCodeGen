using DbAccessCodeGen.Configuration;
using DbAccessCodeGen.Objects;
using Kull.Data;
using Kull.DatabaseMetadata;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Channels;
using System.Data;
using System.Data.Common;

namespace DbAccessCodeGen.CodeGen
{
    public class CodeGenHandler
    {
        public const string Disclaimer = "// This is generated code. Never ever edit this!\r\n\r\n";

        protected readonly Dictionary<string, string> IntegratedTypeMap = new Dictionary<string, string>()
        {
            { "System.Boolean", "bool" },
{ "System.Byte", "byte" },
{ "System.SByte", "sbyte" },
{ "System.Char", "char" },
{ "System.Decimal", "decimal" },
{ "System.Double", "double" },
{ "System.Single", "float" },
{ "System.Int32", "int" },
{ "System.UInt32", "uint" },
{ "System.Int64", "long" },
{ "System.UInt64", "ulong" },
{ "System.Int16", "short" },
{ "System.UInt16", "ushort" },
{ "System.Object", "object" },
{ "System.String", "string" },

        };


        private readonly IServiceProvider serviceProvider;
        private readonly Settings settings;
        private readonly ILogger logger;
        private readonly SPParametersProvider sPParametersProvider;
        private readonly SqlHelper sqlHelper;
        private readonly NamingHandler namingHandler;
        private readonly SqlTypeMapper sqlTypeMapper;
        private readonly DbConnection connection;
        ConcurrentDictionary<DBObjectName, SqlFieldDescription[]> userDefinedTypes = new ConcurrentDictionary<DBObjectName, SqlFieldDescription[]>();

        private object templateLock = new object();
        private volatile bool tempalteParsed = false;
        public string? ModelFileTemplate;
        public string? ServiceClassTemplate;
        public string? ServiceMethodTemplate;


        public CodeGenHandler(IServiceProvider serviceProvider, Configuration.Settings settings, ILogger<CodeGenHandler> logger,
            SPParametersProvider sPParametersProvider,
            SqlHelper sqlHelper,
            NamingHandler namingHandler,
            SqlTypeMapper sqlTypeMapper,
            DbConnection connection)
        {
            this.serviceProvider = serviceProvider;
            this.settings = settings;
            this.logger = logger;
            this.sPParametersProvider = sPParametersProvider;
            this.sqlHelper = sqlHelper;
            this.namingHandler = namingHandler;
            this.sqlTypeMapper = sqlTypeMapper;
            this.connection = connection;
        }

        public void ReadTemplates()
        {

            if (tempalteParsed == false)
            {
                lock (templateLock)
                {
                    logger.LogDebug("Read template files");
                    if (tempalteParsed == false)
                    {
                        ModelFileTemplate = GetTemplate("ModelFile", Templates.DefaultTemplates.ModelFileTemplate);
                        ServiceClassTemplate = GetTemplate("ServiceClass", Templates.DefaultTemplates.ServiceClassTemplate);
                        ServiceMethodTemplate = GetTemplate("ServiceMethod", Templates.DefaultTemplates.ServiceMethodTemplate);
                        tempalteParsed = true;
                    }
                    logger.LogDebug("Finished Read template files");
                }
            }
        }

        private string GetTemplate(string name, string defaultTemplate)
        {
            var folderPath = settings.TemplateDir == null ? "Templates" : settings.TemplateDir;
            string fullName = System.IO.Path.Combine(folderPath, name + ".cs.scriban");
            if (System.IO.File.Exists(fullName))
                return System.IO.File.ReadAllText(fullName);
            return defaultTemplate;
        }

        public async Task ExecuteCodeGen(SPMetadata codeGenPrm, ChannelWriter<(string name, string template)> methods)
        {
            ReadTemplates();
            logger.LogDebug($"Generate code for {codeGenPrm.SqlName}");
            List<Model> modelsToGenerate = new List<Model>();
            Model? parameterModel = codeGenPrm.Parameters.Any() ? new Model(
                codeGenPrm.ParameterTypeName!,
                codeGenPrm.Parameters
                    .Where(d => d.ParameterDirection == System.Data.ParameterDirection.Input || d.ParameterDirection == System.Data.ParameterDirection.InputOutput)
                    .Select(s => GetModelProperty(s)
                ).ToArray()) : null;
            if (parameterModel != null)
                modelsToGenerate.Add(parameterModel);
            Model? resultModel = codeGenPrm.ResultFields != null && codeGenPrm.ResultFields.Any(r => r.Name != null) && codeGenPrm.ResultFields.Any() ? new Model(
                codeGenPrm.ResultType!,
                codeGenPrm.ResultFields.Where(r => r.Name != null).Select(s => GetModelProperty(s)
                ).ToArray()) : null;
            if (resultModel != null)
                modelsToGenerate.Add(resultModel);
            var udtPrms = codeGenPrm.Parameters.Where(u => u.UserDefinedType != null)
                .Select(u => u.UserDefinedType!)
                .Distinct();
            foreach (var item in udtPrms)
            {
                var noFields = Array.Empty<SqlFieldDescription>();
                if (userDefinedTypes.TryAdd(item, noFields))
                {
                    var realFields = await sqlHelper.GetTableTypeFields(connection, item);
                    if (!userDefinedTypes.TryUpdate(item, realFields, noFields))
                    {
                        throw new NotSupportedException("TryUpdate expected to be true");
                    }
                    var udtmodel = new Model(
                namingHandler.GetIdentifierForUserDefinedType(item.Name),
                realFields.Select(s => GetModelProperty(s)).ToArray());

                    modelsToGenerate.Add(udtmodel);
                }
            }

            var modelTemplate = Scriban.Template.Parse(ModelFileTemplate);
            var serviceMethodTemplate = Scriban.Template.Parse(ServiceMethodTemplate);
            foreach (var m in modelsToGenerate)
            {
                var str = await modelTemplate.RenderAsync(m, memberRenamer: member => member.Name);
                str = str.Replace("\t", "    ");
                var (fullOutDir, fileName) = GetPaths(m.Name, true);
                await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(fullOutDir, fileName), Disclaimer + str);
            }
            var serviceMethod = await serviceMethodTemplate.RenderAsync(new
            {
                ResultFields = resultModel?.Properties?.Count == 0 ? null : resultModel?.Properties,
                ResultType = codeGenPrm.ResultType,
                Parameters = parameterModel?.Properties ?? Array.Empty<ModelProperty>(),
                MethodName = codeGenPrm.MethodName,
                SqlName = codeGenPrm.SqlName,
                ParameterTypeName = codeGenPrm.ParameterTypeName,
                settings.GenerateAsyncCode,
                settings.GenerateSyncCode
            }, memberRenamer: member => member.Name);
            serviceMethod = serviceMethod.Replace("\t", "    ");
            serviceMethod = string.Join("\r\n", serviceMethod.Split("\r\n").Select(s => "        " + s));
            await methods.WriteAsync((codeGenPrm.MethodName, serviceMethod));
            logger.LogDebug($"Finished Generate code for {codeGenPrm.SqlName}");
        }

        private (string, string) GetPaths(Identifier name, bool createDir)
        {
            var relativeNamespace =
                                    string.IsNullOrEmpty(name.Namespace) ? null :
                                    name.Namespace.StartsWith(settings.Namespace) ?
                                    name.Namespace == settings.Namespace ? "" : name.Namespace.Substring(settings.Namespace.Length + 1) :
                                    name.Namespace;
            var fullOutDir = relativeNamespace == null ? settings.OutputDir :
                    System.IO.Path.Combine(settings.OutputDir,
                        string.Join("/", relativeNamespace.Split('.')));
            var fileName = name.Name + ".cs";
            if (createDir && !System.IO.Directory.Exists(fullOutDir))
            {
                System.IO.Directory.CreateDirectory(fullOutDir);
            }
            return (fullOutDir, fileName);
        }

        public async Task WriteServiceClass(ChannelReader<(string name, string template)> methods)
        {
            SortedDictionary<string, string> allMethods = new SortedDictionary<string, string>(); ;
            await foreach ((string name, string template) in methods.ReadAllAsync())
            {
                allMethods.Add(name, template);
            };
            string methodsString = string.Join("\r\n\r\n", allMethods.Select(k => k.Value));
            var serviceClassTemplate = Scriban.Template.Parse(ServiceClassTemplate);
            var serviceClassName = namingHandler.GetServiceClassName();
            var serviceString = await serviceClassTemplate.RenderAsync(new
            {
                Name = serviceClassName,
                Methods = methodsString
            }, memberRenamer: m => m.Name);
            var (fullOutDir, fileName) = GetPaths(serviceClassName, true);
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(fullOutDir, fileName), Disclaimer + serviceString);
        }

        private ModelProperty GetModelProperty(SPParameter s)
        {
            return GetModelProperty(GeneratedCodeType.ParameterClass, s.DbType, s.UserDefinedType, s.SqlName, s.IsNullable, s.ParameterDirection);
        }

        private ModelProperty GetModelProperty(SqlFieldDescription s)
        {
            return GetModelProperty(GeneratedCodeType.ResultClass, s.DbType, null, s.Name, s.IsNullable, null);
        }

        private ModelProperty GetModelProperty(GeneratedCodeType generatedCodeType,
            SqlType dbType, DBObjectName? userDefinedType, string sqlName, bool isNullable, ParameterDirection? parameterDirection)
        {
            var type = GetNetType(dbType, userDefinedType);
            var csName = namingHandler.GetPropertyName(sqlName, generatedCodeType);
            return new ModelProperty(
                                sqlName: sqlName,
                                csName: csName,
                                parameterName: namingHandler.GetParameterName(csName),
                                netType: type,
                                nullable: isNullable,
                                getCode: sqlTypeMapper.GetMappingCode(type,
                                    isNullable, csName),
                                parameterDirection: parameterDirection
                                );
        }



        private string GetNetType(SqlType dbType, DBObjectName? userDefinedType)
        {
            if (userDefinedType != null)
            {
                return namingHandler.GetIdentifierForUserDefinedType(userDefinedType).ToString();
            }
            if (IntegratedTypeMap.TryGetValue(dbType.NetType.FullName!, out var netType))
            {
                return netType;
            }

            return dbType.NetType.FullName!;
        }
    }
}
