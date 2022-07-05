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
using DbAccessCodeGen.Templates;

namespace DbAccessCodeGen.CodeGen
{
    public class CodeGenHandler
    {
        public const string Disclaimer = "// This is generated code. Never ever edit this!\r\n\r\n";
        List<string> generatedFileList = new();
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
        ConcurrentDictionary<DBObjectName, IReadOnlyCollection<SqlFieldDescription>> userDefinedTypes = new();

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
                        ModelFileTemplate = GetTemplate("ModelFile");
                        ServiceClassTemplate = GetTemplate("ServiceClass");
                        ServiceMethodTemplate = GetTemplate("ServiceMethod");
                        tempalteParsed = true;
                    }
                    logger.LogDebug("Finished Read template files");
                }
            }
        }

        private string GetTemplate(string name)
        {
            var folderPath = settings.TemplateDir == null ? "Templates" : settings.TemplateDir;
            string fullName = System.IO.Path.Combine(folderPath, name + ".cs.scriban");
            if (System.IO.File.Exists(fullName))
                return System.IO.File.ReadAllText(fullName);
            return TemplateRetrieval.GetTemplate(name);
        }

        enum CodeGenerationType { Sync, Async, StreamAsync }

        private string GetFullResultType(DBOperationResultType dBOperationResultType, CodeGenerationType codeGenerationType, Identifier baseResultType)
        {
            if (dBOperationResultType == DBOperationResultType.AffectedRows)
            {
                string resType = "(int AffectedRows, int ReturnValue)";
                if (codeGenerationType == CodeGenerationType.Sync)
                {
                    return resType;
                }
                else
                {
                    return "Task<" + resType + ">";
                }
            }
            else if (dBOperationResultType == DBOperationResultType.Result)
            {
                switch (codeGenerationType)
                {
                    case CodeGenerationType.Sync:
                        return $"IEnumerable<{baseResultType}>";
                    case CodeGenerationType.Async:
                        return $"Task<IEnumerable<{baseResultType}>>";
                    case CodeGenerationType.StreamAsync:
                        return $"IAsyncEnumerable<{baseResultType}>";
                    default:
                        break;
                }
            }
            else if (dBOperationResultType == DBOperationResultType.Reader)
            {
                string resType = "DbDataReader";
                if (codeGenerationType == CodeGenerationType.Sync)
                {
                    return resType;
                }
                else
                {
                    return "Task<" + resType + ">";
                }
            }
            else if (dBOperationResultType == DBOperationResultType.Dictionary)
            {
                string resType = "IEnumerable<Dictionary<string, object?>>";
                if (codeGenerationType == CodeGenerationType.Sync)
                {
                    return resType;
                }
                else
                {
                    return "Task<" + resType + ">";
                }
            }
            throw new InvalidOperationException("Cannot generate code");
        }

        public async Task ExecuteCodeGen(DbOperationMetadata codeGenPrm, ChannelWriter<(string name, string template)> methods)
        {
            ReadTemplates();
            var customMappings = codeGenPrm.Settings.CustomTypeMappings ?? this.settings.CustomTypeMappings;

            logger.LogDebug($"Generate code for {codeGenPrm.SqlName}");
            List<Model> modelsToGenerate = new List<Model>();
            var udtPrms = codeGenPrm.Parameters.Where(u => u.UserDefinedType != null)
                .Select(u => u.UserDefinedType!)
                .Distinct();
            Dictionary<DBObjectName, Model> userGeneratedTypes = new Dictionary<DBObjectName, Model>();
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
                realFields.Select(s => GetModelProperty(s, customMappings)).ToArray(),
                codeType: GeneratedCodeType.TableValuedParameter);
                    userGeneratedTypes.Add(item, udtmodel);
                    modelsToGenerate.Add(udtmodel);
                }
            }

            Model? parameterModel = codeGenPrm.Parameters.Any() ? new Model(
                codeGenPrm.ParameterTypeName!,
                codeGenPrm.Parameters
                    .Where(d => d.ParameterDirection == System.Data.ParameterDirection.Input || d.ParameterDirection == System.Data.ParameterDirection.InputOutput)
                    .Select(s => GetModelProperty(s, s.UserDefinedType == null ? null : userGeneratedTypes[s.UserDefinedType], customMappings)
                ).ToArray(),
                codeType: GeneratedCodeType.ParameterClass) : null;
            if (parameterModel != null)
                modelsToGenerate.Add(parameterModel);
            Model? resultModel = codeGenPrm.ResultFields != null && codeGenPrm.ResultFields.Any(r => r.Name != null) && codeGenPrm.ResultFields.Any() ? new Model(
                codeGenPrm.ResultType!,
                codeGenPrm.ResultFields.Where(r => r.Name != null).Select(s => GetModelProperty(s, customMappings)
                ).ToArray(),
                codeType: GeneratedCodeType.ResultClass) : null;
            if (resultModel != null)
                modelsToGenerate.Add(resultModel);


            var modelTemplate = Scriban.Template.Parse(ModelFileTemplate);
            ValidateTemplate(modelTemplate, "ModelFile");
            var serviceMethodTemplate = Scriban.Template.Parse(ServiceMethodTemplate);
            ValidateTemplate(serviceMethodTemplate, "ServiceMethod");
            foreach (var m in modelsToGenerate)
            {
                var str = await modelTemplate.RenderAsync(m, memberRenamer: member => member.Name);
                str = str.Replace("\t", "    ");
                var (fullOutDir, fileName) = GetPaths(m.Name, true);
                await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(fullOutDir, fileName), Disclaimer + str);
                generatedFileList.Add(System.IO.Path.Combine(fullOutDir, fileName));
            }
            var serviceMethod = await serviceMethodTemplate.RenderAsync(new
            {
                ResultFields = resultModel?.Properties?.Count == 0 ? null : resultModel?.Properties,
                ResultType = codeGenPrm.ResultType,
                Parameters = parameterModel?.Properties ?? Array.Empty<ModelProperty>(),
                OutputParameters = (parameterModel?.Properties ?? Array.Empty<ModelProperty>()).Where(p => p.ParameterDirection == ParameterDirection.Output || p.ParameterDirection == ParameterDirection.InputOutput).ToList(),
                MethodName = codeGenPrm.MethodName,
                SqlName = codeGenPrm.SqlName,
                CommandText = codeGenPrm.CommandText,
                CommandType = codeGenPrm.CommandType,
                ParameterTypeName = codeGenPrm.ParameterTypeName,
                ReplaceParameters = codeGenPrm.ReplaceParameters,
                GenerateAsyncCode = GetRealAsyncType(codeGenPrm.Settings.GenerateAsyncStreamCode ?? settings.GenerateAsyncStreamCode, codeGenPrm.Settings.GenerateAsyncCode ?? settings.GenerateAsyncCode, CodeGenerationType.Async, codeGenPrm.Settings.ResultType),
                GenerateSyncCode = codeGenPrm.Settings.GenerateSyncCode ?? settings.GenerateSyncCode,
                GenerateAsyncStreamCode = GetRealAsyncType(codeGenPrm.Settings.GenerateAsyncStreamCode ?? settings.GenerateAsyncStreamCode, codeGenPrm.Settings.GenerateAsyncCode ?? settings.GenerateAsyncCode, CodeGenerationType.StreamAsync, codeGenPrm.Settings.ResultType),
                ExecuteOnly = codeGenPrm.Settings.ResultType == DBOperationResultType.AffectedRows,
                ReturnResult =
                    codeGenPrm.Settings.ResultType == DBOperationResultType.Result && (resultModel?.Properties?.Count ?? 0) > 0,
                ReturnReader = codeGenPrm.Settings.ResultType == DBOperationResultType.Reader,
                OperationResultType = codeGenPrm.Settings.ResultType,
                FullStreamAsyncResultType = GetFullResultType(codeGenPrm.Settings.ResultType, CodeGenerationType.StreamAsync, codeGenPrm.ResultType),
                FullAsyncResultType = GetFullResultType(codeGenPrm.Settings.ResultType, CodeGenerationType.Async, codeGenPrm.ResultType),
                FullSyncResultType = GetFullResultType(codeGenPrm.Settings.ResultType, CodeGenerationType.Sync, codeGenPrm.ResultType),

            }, memberRenamer: member => member.Name);
            serviceMethod = serviceMethod.Replace("\t", "    ");
            serviceMethod = string.Join("\r\n", serviceMethod.Split("\r\n").Select(s => "        " + s));
            await methods.WriteAsync((codeGenPrm.MethodName, serviceMethod));
            logger.LogDebug($"Finished Generate code for {codeGenPrm.SqlName}");
        }

        private bool GetRealAsyncType(bool asyncStreamCode, bool asyncCode, CodeGenerationType codeGenType, DBOperationResultType resultType)
        {
            if (resultType == DBOperationResultType.AffectedRows || resultType == DBOperationResultType.Reader || resultType == DBOperationResultType.Dictionary)
            {//Stream async makes no sense for these result types, therefore map them to Async
                if (codeGenType == CodeGenerationType.Async)
                {
                    return asyncCode||asyncStreamCode;
                }
                if (codeGenType == CodeGenerationType.StreamAsync)
                {
                    return false;
                }
            }
            if (codeGenType == CodeGenerationType.Async)
            {
                return asyncCode;
            }
            if (codeGenType == CodeGenerationType.StreamAsync)
            {
                return asyncStreamCode;
            }
            throw new InvalidOperationException("not supported");
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
            ValidateTemplate(serviceClassTemplate, "ServiceClass");
            var serviceClassName = namingHandler.GetServiceClassName();
            var serviceString = await serviceClassTemplate.RenderAsync(new
            {
                Name = serviceClassName,
                Methods = methodsString,
                ConstructorVisibility = settings.ConstructorVisibility,
                ServiceClassModifiers = settings.ServiceClassModifiers
            }, memberRenamer: m => m.Name);
            var (fullOutDir, fileName) = GetPaths(serviceClassName, true);
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(fullOutDir, fileName), Disclaimer + serviceString);
            generatedFileList.Add(System.IO.Path.Combine(fullOutDir, fileName));
        }

        public void CleanupDirectory()
        {
            var files = System.IO.Directory.GetFiles(settings.OutputDir, "*.cs", SearchOption.AllDirectories);
            var genFiles = generatedFileList.Select(s => s.Replace('\\', '/').ToLower()).ToArray();
            char[] buffer = new char[Disclaimer.Length];
            foreach (var file in files)
            {
                if (!genFiles.Contains(file.Replace('\\', '/').ToLower()) && new FileInfo(file).Length >= Disclaimer.Length)
                {
                    bool delete = false;
                    using (var f = File.OpenText(file))
                    {
                        if (f.Read(buffer, 0, buffer.Length) == buffer.Length)
                        {
                            if (new String(buffer, 0, buffer.Length) == Disclaimer)
                            {
                                delete = true;
                            }
                        }
                    }
                    if (delete)
                    {
                        File.Delete(file);
                        logger.LogInformation($"deleted {file}");
                    }
                }
            }
        }

        private void ValidateTemplate(Scriban.Template template, string templateName)
        {
            if (template.HasErrors)
            {
                Console.Error.WriteLine("ERRORS for " + templateName);
                foreach (var m in template.Messages)
                {
                    if (m.Type == Scriban.Parsing.ParserMessageType.Error)
                    {
                        Console.Error.WriteLine(m.Span.ToStringSimple() + ": " + m.Message);
                    }
                    else if (m.Type == Scriban.Parsing.ParserMessageType.Warning)
                    {
                        Console.Error.WriteLine("WARNING: " + m.Span.ToStringSimple() + ": " + m.Message);
                    }
                }
                throw new SyntaxErrorException("Template error for " + templateName);
            }
        }

        private ModelProperty GetModelProperty(SPParameter s, Model? userDefinedType,
            IReadOnlyDictionary<string, string> customTypeMappings)
        {
            return GetModelProperty(GeneratedCodeType.ParameterClass, s.DbType, s.MaxLength, s.UserDefinedType, userDefinedType, s.SqlName, s.IsNullable, s.ParameterDirection,
                customTypeMappings);
        }

        private ModelProperty GetModelProperty(SqlFieldDescription s,
                IReadOnlyDictionary<string, string> customTypeMappings)
        {
            return GetModelProperty(GeneratedCodeType.ResultClass, s.DbType, s.MaxLength, null, null, s.Name, s.IsNullable, null, customTypeMappings);
        }

        private ModelProperty GetModelProperty(GeneratedCodeType generatedCodeType,
            SqlType dbType, int? dbSize, DBObjectName? userDefinedType, Model? userDefinedTypeGen, string sqlName, bool isNullable, ParameterDirection? parameterDirection,
            IReadOnlyDictionary<string, string> customTypeMappings)
        {
            var type = customTypeMappings.ContainsKey(dbType.DbType) ?
                customTypeMappings[dbType.DbType] : GetNetType(dbType, userDefinedType);
            var csName = namingHandler.GetPropertyName(sqlName, generatedCodeType);
            return new ModelProperty(
                                sqlName: sqlName,
                                csName: csName,
                                parameterName: namingHandler.GetParameterName(csName),
                                netType: type,
                                nullable: isNullable,
                                getCode: sqlTypeMapper.GetMappingCode(type,
                                    isNullable, csName),
                                parameterDirection: parameterDirection,
                                userDefinedTableType: userDefinedTypeGen,
                                sqlType: dbType,
                                size: dbSize
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
