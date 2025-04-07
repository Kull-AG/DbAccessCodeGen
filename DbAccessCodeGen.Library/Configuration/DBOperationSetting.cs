using Kull.Data;
using Kull.DatabaseMetadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbAccessCodeGen.Configuration
{
    public enum DBOperationResultType
    {
        Result=1,
        AffectedRows=2,
        Reader=3,
        Dictionary=4
    }

    public partial class SpecificDBNaming
    {
        public string DBSchemaName { get; private set; }
        public string DBObjectName { get; private set; }
        public static SpecificDBNaming? CreateSpecifcDBNaming(string? dbSchemaName, string? dbObjectName)
        {
            if (!string.IsNullOrEmpty(dbObjectName) && !string.IsNullOrEmpty(dbSchemaName))
            {
                return new SpecificDBNaming()
                {
                    DBSchemaName = dbSchemaName,
                    DBObjectName = dbObjectName,
                };
            }
            return null;
        }
    }

    public partial class DBOperationSetting
    {
        public DBOperationSetting(string? dbObjectName,
            DBObjectType objectType,
            string? methodName,
            IReadOnlyDictionary<string, object?>? executeParameters, IReadOnlyCollection<string>? ignoreParameters,
            SpecificDBNaming? specificDBNaming)
        {
            this.DBObjectType = objectType;

            //Check if specific db object naming is set
            if (specificDBNaming != null) {
                DBObjectName = new DBObjectName(specificDBNaming.DBSchemaName, specificDBNaming.DBObjectName);
            }
            else
            {
                DBObjectName = dbObjectName ?? throw new ArgumentNullException(nameof(dbObjectName));
                if (DBObjectName.Schema == null)
                {
                    DBObjectName = new DBObjectName("dbo" /* we assume default schema dbo */, DBObjectName.Name);
                }
            }

            ExecuteParameters = executeParameters;
            IgnoreParameters = ignoreParameters;
            MethodName = methodName;
        }

        public DBObjectName DBObjectName { get; init; }

        public DBObjectType DBObjectType { get; init; }

        public string? MethodName { get; init; }

        public IReadOnlyDictionary<string, object?>? ExecuteParameters { get; init; }
        public IReadOnlyCollection<string>? IgnoreParameters { get; }

        public IReadOnlyCollection<string>? IgnoreFields { get; init; }
        public IReadOnlyDictionary<string, string>? ReplaceParameters { get; init; }

        public IReadOnlyDictionary<string, string>? CustomTypeMappings { get; init; }
        public bool? GenerateAsyncCode { get; init; } = null;
        public bool? GenerateAsyncStreamCode { get; init; } = null;
        public bool? GenerateSyncCode { get; init; } = null;

        public DBOperationResultType ResultType { get; init; } = DBOperationResultType.Result;

        /// <summary>
        /// Option to set the specific naming of the object
        /// Warning: Could be null
        /// </summary>
        public SpecificDBNaming? SpecificDBNaming { get; init; }


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
                string? dbObjectName;
                DBObjectType type;
                if (os.ContainsKey("SP"))
                {
                    dbObjectName = (string?)os["SP"];
                    type = DBObjectType.StoredProcedure;
                }
                else if (os.ContainsKey("View"))
                {
                    dbObjectName = (string?)os["View"];
                    type = DBObjectType.TableOrView;
                }
                else
                {
                    throw new InvalidOperationException("Must set either SP or View, but never both");
                }

                return new DBOperationSetting(dbObjectName,
                    type,
                    methodName: os.GetOrThrow("MethodName", (string?)null),
                    executeParameters: (IReadOnlyDictionary<string, object?>?)executeParameters, ignoreParameters: (IReadOnlyCollection<string>?)ignoreParameters,
                    specificDBNaming: GetSpecificDBObjectNaming(os)
                    )
                {
                    GenerateSyncCode = os.GetOrThrow<bool?>(nameof(GenerateSyncCode), null),
                    GenerateAsyncCode = os.GetOrThrow<bool?>(nameof(GenerateAsyncCode), null),
                    GenerateAsyncStreamCode = os.GetOrThrow<bool?>(nameof(GenerateAsyncStreamCode), null),
                    ReplaceParameters = replaceParameters,
                    CustomTypeMappings = os.GetOrThrow<IReadOnlyDictionary<string, string>?>(nameof(CustomTypeMappings), null),
                    IgnoreFields = os.GetOrThrow<IReadOnlyCollection<string>?>(nameof(IgnoreFields), null),
                    ResultType = os.GetOrThrow<bool>("ExecuteOnly", false) ? DBOperationResultType.AffectedRows
                        : os.GetOrThrow<DBOperationResultType>("ResultType", DBOperationResultType.Result),
                };
            }
            if (obj is string s)
                return new DBOperationSetting(s, DBObjectType.StoredProcedure, methodName: null, executeParameters: null, ignoreParameters: null, specificDBNaming: null);
            throw new NotSupportedException($"{obj} is not a proc");
        }

        /// <summary>
        /// Checks if the specific Naming is available. If not available it returns null.
        /// The schema name and the object name must be at least the length of 1
        /// </summary>
        /// <returns>SpecificDBnaming Object if the schema and object name is available</returns>
        private static SpecificDBNaming? GetSpecificDBObjectNaming(IReadOnlyDictionary<string, object> os)
        {
            if (os == null) return null;

            if (os.ContainsKey("SpecificDBNaming"))
            {
                if (os["SpecificDBNaming"] is IReadOnlyDictionary<object, object> od)
                {
                    var specificDBObjectDict = od.ToDictionary(k => k.Key.ToString()!, k => k.Value) ?? throw new ArgumentNullException("Error on extracting the information from object 'SpecificDBNaming'");
                    string? dbSchemaName = specificDBObjectDict.GetOrThrow<string?>("DBSchemaName", null);
                    string? dbObjectName = specificDBObjectDict.GetOrThrow<string?>("DBObjectName", null);
                    return SpecificDBNaming.CreateSpecifcDBNaming(dbSchemaName, dbObjectName);
                }
            }
            return null;
        }
    }
}
