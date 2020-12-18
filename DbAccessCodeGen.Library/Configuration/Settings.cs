using System;
using System.Collections.Generic;
using System.Linq;

namespace DbAccessCodeGen.Configuration
{
    public record Settings
    {
        public Settings(string? connectionString, string @namespace,
            IReadOnlyCollection<ProcedureSetting> procedures,
            IReadOnlyCollection<string>? ignoreParameters,
            string outputDir, bool generateAsyncCode, bool generateSyncCode,
            string? serviceClassName, string? templateDir, string? namingJS)
        {
            ConnectionString = connectionString;
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            Procedures = procedures ?? throw new ArgumentNullException(nameof(procedures));
            OutputDir = outputDir ?? throw new ArgumentNullException(nameof(outputDir));
            GenerateAsyncCode = generateAsyncCode;
            GenerateSyncCode = generateSyncCode;
            ServiceClassName = serviceClassName;
            TemplateDir = templateDir;
            NamingJS = namingJS;
            IgnoreParameters = ignoreParameters ?? Array.Empty<string>();
        }

        public string? ConnectionString { get; init; }
        public string Namespace { get; } = "DbAccess";
        public IReadOnlyCollection<ProcedureSetting> Procedures { get; }

        public IReadOnlyCollection<string> IgnoreParameters { get; init; }
        public IReadOnlyDictionary<string, string> ReplaceParameters { get; init; } = new Dictionary<string, string>();

        public string OutputDir { get; } = "DbAccess";

        public string PersistResultPath { get; init; } = "ResultSets";

        public bool GenerateAsyncStreamCode { get; init; } = false;
        public bool GenerateAsyncCode { get; } = true;
        public bool GenerateSyncCode { get; } = false;
        public bool AlwaysAllowNullForStrings { get; init; } = true;

        public string? ServiceClassName { get; }

        public string? TemplateDir { get; }

        /// <summary>
        /// The modifiers for the service class. defaults to "public partial"
        /// Use this if you want an abstract service class or make it internal only
        /// </summary>
        public string ServiceClassModifiers { get; init; } = "public partial";

        /// <summary>
        /// The visibility of the constructor. Defaults to public
        /// Use this to set it to private to use you own constructor
        /// </summary>
        public string ConstructorVisibility { get; init; } = "public";

        /// <summary>
        /// Path to a javascript file containing naming convention.
        /// </summary>
        public string? NamingJS { get; }

        public static Settings FromObject(object obj)
        {
            if (obj is IReadOnlyDictionary<object, object> od)
            {
                return FromObject(od.ToDictionary(k => k.Key.ToString()!, k => k.Value));
            }
            if (obj is IReadOnlyDictionary<string, object> os)
            {
                var procs = os["Procedures"];
                if (procs == null)
                {
                    throw new ArgumentNullException("Procedures");
                }
                if (procs is not IEnumerable<object> proclist)
                {
                    throw new InvalidOperationException($"Procedures must be list but is {procs.GetType().FullName}");
                }
                var ignoreParameters = os.ContainsKey(nameof(IgnoreParameters)) ? os[nameof(IgnoreParameters)] : null;
                if (ignoreParameters is IReadOnlyCollection<object> o)
                {
                    ignoreParameters = o.Select(o => (string)Convert.ChangeType(o, typeof(string))).ToArray();
                }
                var replaceParameters = os.GetOrThrow<IReadOnlyDictionary<string, string>>(nameof(ReplaceParameters), new Dictionary<string, string>());
                return new Settings(os.GetOrThrow<string?>("ConnectionString", null),
                    os.GetOrThrow<string>("Namespace", "DbAccess"),
                    proclist.Select(s => ProcedureSetting.FromObject(s)).ToList(),
                    ignoreParameters: (IReadOnlyCollection<string>?)ignoreParameters,
                    outputDir: os.GetOrThrow<string>("OutputDir", "DbAccess"),
                    generateAsyncCode: os.GetOrThrow(nameof(GenerateAsyncCode), true),
                    generateSyncCode: os.GetOrThrow(nameof(GenerateSyncCode), true),
                    serviceClassName: os.GetOrThrow<string?>(nameof(ServiceClassName), null),
                    templateDir: os.GetOrThrow<string?>(nameof(TemplateDir), null),
                    namingJS: os.GetOrThrow<string?>(nameof(NamingJS), null)
                    )
                {
                    AlwaysAllowNullForStrings = os.GetOrThrow(nameof(AlwaysAllowNullForStrings), true),
                    GenerateAsyncStreamCode = os.GetOrThrow(nameof(GenerateAsyncStreamCode), false),
                    PersistResultPath = os.GetOrThrow(nameof(PersistResultPath), "ResultSets"),
                    ServiceClassModifiers = os.GetOrThrow(nameof(ServiceClassModifiers), "public partial"),
                    ConstructorVisibility = os.GetOrThrow(nameof(ConstructorVisibility), "public"),
                    ReplaceParameters = replaceParameters
                };
            }
            throw new NotSupportedException("Must be object at root");
        }
    }
}
