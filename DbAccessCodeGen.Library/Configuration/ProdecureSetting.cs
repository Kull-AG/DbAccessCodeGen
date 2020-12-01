using Kull.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbAccessCodeGen.Configuration
{
    public class ProdecureSetting
    {
        public ProdecureSetting(string storedProcedure, IReadOnlyDictionary<string, object>? executeParameters)
        {
            StoredProcedure = storedProcedure ?? throw new ArgumentNullException(nameof(storedProcedure));
            ExecuteParameters = executeParameters;
        }

        public DBObjectName StoredProcedure { get; init; }
        public IReadOnlyDictionary<string, object>? ExecuteParameters { get; init; }

        public static ProdecureSetting FromObject(object obj)
        {
            if (obj is IDictionary<object, object> od)
            {
                return FromObject(od.ToDictionary(k => k.Key.ToString()!, k => k.Value));
            }
            if (obj is IDictionary<string, object> os)
            {
                var executeParameters = os.ContainsKey("ExecuteParameters") ? os["ExecuteParameters"] : null;
                if(executeParameters is IDictionary<object, object> oe)
                {
                    executeParameters = oe.ToDictionary(k => k.Key.ToString()!, k => k.Value);
                }
                return new ProdecureSetting((string)os["SP"], (IReadOnlyDictionary<string, object>?)executeParameters);
            }
            if (obj is string s)
                return new ProdecureSetting(s, null);
            throw new NotSupportedException($"{obj} is not a proc");
        }
    }
}
