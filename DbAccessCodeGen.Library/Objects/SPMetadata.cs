using Kull.Data;
using Kull.DatabaseMetadata;
using System.Collections.Generic;
using System.Linq;

namespace DbAccessCodeGen.Objects
{
    public class SPMetadata
    {
        public IReadOnlyCollection<SPParameter> Parameters { get; }
        public IReadOnlyCollection<SqlFieldDescription>? ResultFields { get; }


        public DBObjectName SqlName { get; }

        public string MethodName { get; }

        public Identifier? ResultType { get; }

        public Identifier? ParameterTypeName { get; }
        public SPMetadata(DBObjectName name, IReadOnlyCollection<SPParameter> parameters, IReadOnlyCollection<SqlFieldDescription> fields,
                string methodName,
                Identifier resultType,
                Identifier parameterTypeName)
        {
            this.SqlName = name;
            this.Parameters = parameters;
            this.ResultFields = fields.Count(f => f.Name != null) == 0 ? null : fields;
            this.MethodName = methodName;
            this.ResultType = fields.Count(f => f.Name != null) == 0 ? new Identifier("", "Dictionary<string, object?>") : resultType;
            this.ParameterTypeName = parameters.Count == 0 ? null : parameterTypeName;
        }
    }

}
