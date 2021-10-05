using Kull.Data;
using Kull.DatabaseMetadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbAccessCodeGen.Configuration
{
    public partial class DBOperationSetting
    {
        public DBOperationSetting(string dbObjectName,
            DBObjectType objectType,

            IReadOnlyDictionary<string, object?>? executeParameters, IReadOnlyCollection<string>? ignoreParameters)
        {
            this.DBObjectType = objectType;
            DBObjectName = dbObjectName ?? throw new ArgumentNullException(nameof(dbObjectName));
            if (DBObjectName.Schema == null)
            {
                DBObjectName = new DBObjectName("dbo" /* we assume default schema dbo */, DBObjectName.Name);
            }
            ExecuteParameters = executeParameters;
            IgnoreParameters = ignoreParameters;
        }

        public DBObjectName DBObjectName { get; init; }

        public DBObjectType DBObjectType { get; init; }

        public IReadOnlyDictionary<string, object?>? ExecuteParameters { get; init; }
        public IReadOnlyCollection<string>? IgnoreParameters { get; }

        public IReadOnlyCollection<string>? IgnoreFields { get; init; }
        public IReadOnlyDictionary<string, string>? ReplaceParameters { get; init; }

        public IReadOnlyDictionary<string, string>? CustomTypeMappings { get; init; }
        public bool? GenerateAsyncCode { get; init; } = null;
        public bool? GenerateAsyncStreamCode { get; init; } = null;
        public bool? GenerateSyncCode { get; init; } = null;

        public bool ExecuteOnly { get; set; } = false;

        public static DBOperationSetting FromObject(object obj)
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
                string dbObjectName;
                DBObjectType type;
                if (os.ContainsKey("SP"))
                {
                    dbObjectName = (string)os["SP"];
                    type = DBObjectType.StoredProcedure;
                }
                else if (os.ContainsKey("View"))
                {
                    dbObjectName = (string)os["View"];
                    type = DBObjectType.TableOrView;
                }
                else
                {
                    throw new InvalidOperationException("Must set either SP or View, but never both");
                }
                return new DBOperationSetting(dbObjectName, 
                    type,
                    (IReadOnlyDictionary<string, object?>?)executeParameters, (IReadOnlyCollection<string>?)ignoreParameters)
                {
                    GenerateSyncCode = os.GetOrThrow<bool?>(nameof(GenerateSyncCode), null),
                    GenerateAsyncCode = os.GetOrThrow<bool?>(nameof(GenerateAsyncCode), null),
                    GenerateAsyncStreamCode = os.GetOrThrow<bool?>(nameof(GenerateAsyncStreamCode), null),
                    ReplaceParameters = replaceParameters,
                    CustomTypeMappings = os.GetOrThrow<IReadOnlyDictionary<string, string>?>(nameof(CustomTypeMappings), null),
                    IgnoreFields = os.GetOrThrow<IReadOnlyCollection<string>?>(nameof(IgnoreFields), null),
                    ExecuteOnly = os.GetOrThrow<bool>(nameof(ExecuteOnly), false)
            };
            }
            if (obj is string s)
                return new DBOperationSetting(s, DBObjectType.StoredProcedure, null, null);
            throw new NotSupportedException($"{obj} is not a proc");
        }
    }
}
