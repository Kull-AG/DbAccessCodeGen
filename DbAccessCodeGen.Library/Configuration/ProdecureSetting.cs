using Kull.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbAccessCodeGen.Configuration
{
    public class ProdecureSetting
    {
        public ProdecureSetting(string storedProcedure, IReadOnlyDictionary<string, object>? executeParameters, IReadOnlyCollection<string>? ignoreParameters)
        {
            StoredProcedure = storedProcedure ?? throw new ArgumentNullException(nameof(storedProcedure));
            ExecuteParameters = executeParameters;
            IgnoreParameters = ignoreParameters ?? Array.Empty<string>();
        }

        public DBObjectName StoredProcedure { get; init; }
        public IReadOnlyDictionary<string, object>? ExecuteParameters { get; init; }
        public IReadOnlyCollection<string> IgnoreParameters { get; }

        public static ProdecureSetting FromObject(object obj)
        {
            if (obj is IDictionary<object, object> od)
            {
                return FromObject(od.ToDictionary(k => k.Key.ToString()!, k => k.Value));
            }
            if (obj is IDictionary<string, object> os)
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
                return new ProdecureSetting((string)os["SP"], (IReadOnlyDictionary<string, object>?)executeParameters, (IReadOnlyCollection<string>?)ignoreParameters);
            }
            if (obj is string s)
                return new ProdecureSetting(s, null, null);
            throw new NotSupportedException($"{obj} is not a proc");
        }
    }
}
