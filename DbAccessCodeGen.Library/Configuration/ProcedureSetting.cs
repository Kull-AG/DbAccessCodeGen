using Kull.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbAccessCodeGen.Configuration
{
    public class ProcedureSetting
    {
        public ProcedureSetting(string storedProcedure, IReadOnlyDictionary<string, object?>? executeParameters, IReadOnlyCollection<string>? ignoreParameters)
        {
            StoredProcedure = storedProcedure ?? throw new ArgumentNullException(nameof(storedProcedure));
            ExecuteParameters = executeParameters;
            IgnoreParameters = ignoreParameters;
        }

        public DBObjectName StoredProcedure { get; init; }
        public IReadOnlyDictionary<string, object?>? ExecuteParameters { get; init; }
        public IReadOnlyCollection<string>? IgnoreParameters { get; }
        public IReadOnlyDictionary<string, string>? ReplaceParameters { get; init; }

        public IReadOnlyDictionary<string, string>? CustomTypeMappings { get; init; }
        public bool? GenerateAsyncCode { get; init; } = null;
        public bool? GenerateAsyncStreamCode { get; init; } = null;
        public bool? GenerateSyncCode { get; init; } = null;

        public bool ExecuteOnly { get; set; } = false;

        public static ProcedureSetting FromObject(object obj)
        {
            if (obj is IReadOnlyDictionary<object, object> od)
            {
                return FromObject(od.ToDictionary(k => k.Key.ToString()!, k => k.Value));
            }
            if (obj is IReadOnlyDictionary<string, object> os)
            {
                var executeParameters = os.ContainsKey("ExecuteParameters") ? os["ExecuteParameters"] : null;
                var ignoreParameters = os.ContainsKey(nameof(IgnoreParameters)) ? os[nameof(IgnoreParameters)] : null;
                if (executeParameters is IReadOnlyDictionary<object, object> oe)
                {
                    executeParameters = oe.ToDictionary(k => k.Key.ToString()!, k => k.Value);
                }
                if (ignoreParameters is IReadOnlyCollection<object> o)
                {
                    ignoreParameters = o.Select(o => (string)Convert.ChangeType(o, typeof(string))).ToArray();
                }
                var replaceParameters = os.GetOrThrow<IReadOnlyDictionary<string, string>?>(nameof(ReplaceParameters), null);
                return new ProcedureSetting((string)os["SP"], (IReadOnlyDictionary<string, object?>?)executeParameters, (IReadOnlyCollection<string>?)ignoreParameters)
                {
                    GenerateSyncCode = os.GetOrThrow<bool?>(nameof(GenerateSyncCode), null),
                    GenerateAsyncCode = os.GetOrThrow<bool?>(nameof(GenerateAsyncCode), null),
                    GenerateAsyncStreamCode = os.GetOrThrow<bool?>(nameof(GenerateAsyncStreamCode), null),
                    ReplaceParameters = replaceParameters,
                    CustomTypeMappings = os.GetOrThrow<IReadOnlyDictionary<string, string>?>(nameof(CustomTypeMappings), null),
                    ExecuteOnly = os.GetOrThrow<bool>(nameof(ExecuteOnly), false)
            };
            }
            if (obj is string s)
                return new ProcedureSetting(s, null, null);
            throw new NotSupportedException($"{obj} is not a proc");
        }
    }
}
