using System;
using System.Collections.Generic;
using System.Linq;

namespace DbAccessCodeGen.Configuration
{
    public record Settings
    {
        public Settings(string? connectionString, string @namespace, IReadOnlyCollection<ProdecureSetting> procedures, string outputDir, bool generateAsyncCode, bool generateSyncCode, string? serviceClassName, string? templateDir, string? namingJS)
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
        }

        public string? ConnectionString { get; init; } 
        public string Namespace { get;  } = "DbAccess";
        public IReadOnlyCollection<ProdecureSetting> Procedures { get; }

        public string OutputDir { get;  } = "DbAccess";

        public bool GenerateAsyncCode { get;  } = true;
        public bool GenerateSyncCode { get;  } = false;
        public bool AlwaysAllowNullForStrings { get; init; } = true;

        public string? ServiceClassName { get;  }

        public string? TemplateDir { get;  }

        /// <summary>
        /// Path to a javascript file containing naming convention.
        /// </summary>
        public string? NamingJS { get; }

        public static Settings FromObject(object obj)
        {
            if(obj is IReadOnlyDictionary<object, object> od)
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
                if(procs is not IEnumerable<object> proclist)
                {
                    throw new InvalidOperationException($"Procedures must be list but is {procs.GetType().FullName}");
                }
                return new Settings(os.GetOrThrow<string?>("ConnectionString", null),
                    os.GetOrThrow<string>("Namespace", "DbAccess"),
                    proclist.Select(s => ProdecureSetting.FromObject(s)).ToList(),
                    os.GetOrThrow<string>("OutputDir", "DbAccess"),
                    os.GetOrThrow(nameof(GenerateAsyncCode), true),
                    os.GetOrThrow(nameof(GenerateSyncCode), true),
                    os.GetOrThrow<string?>(nameof(ServiceClassName), null),
                    os.GetOrThrow<string?>(nameof(TemplateDir), null),
                    os.GetOrThrow<string?>(nameof(NamingJS), null)
                    )
                {
                    AlwaysAllowNullForStrings = os.GetOrThrow(nameof(AlwaysAllowNullForStrings), true)
                };
            }
            throw new NotSupportedException("Must be object at root");
        }
    }
}
