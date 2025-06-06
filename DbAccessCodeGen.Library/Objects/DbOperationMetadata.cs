﻿using DbAccessCodeGen.Configuration;
using Kull.Data;
using Kull.DatabaseMetadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbAccessCodeGen.Objects
{
    public class DbOperationMetadata
    {
        public IReadOnlyCollection<SPParameter> Parameters { get; }
        public IReadOnlyDictionary<string, string> ReplaceParameters { get; }
        public ResultSource FieldSource { get; }
        public IReadOnlyCollection<SqlFieldDescription>? ResultFields { get; }


        public DBObjectName SqlName { get; }

        public string CommandText { get; }

        public string CommandType { get; } = "CommandType.StoredProcedure";

        public string MethodName { get; }

        public Identifier? ResultType { get; }

        public Identifier? ParameterTypeName { get; }
        
        public DBOperationSetting Settings {get;}

        public DBOperationResultType DBOperationResultType { get; }

        public DbOperationMetadata(DBObjectName name,
                DBObjectType dBObjectType, 
                IReadOnlyCollection<SPParameter> parameters,
                IReadOnlyDictionary<string, string> replaceParameters,
                ResultSource fieldSource,
            IReadOnlyCollection<SqlFieldDescription> fields,
                string methodName,
                Identifier resultType,
                Identifier parameterTypeName,
                DBOperationSetting setting,
                DBOperationResultType dBOperationResultType)
        {
            this.CommandType = dBObjectType == DBObjectType.StoredProcedure ?
                    "CommandType.StoredProcedure":
                    "CommandType.Text";
            this.CommandText = dBObjectType == DBObjectType.StoredProcedure ?
                    AddSquareBracket(name) : "SELECT * FROM " + name.ToString(false, true);
            this.SqlName = dBObjectType== DBObjectType.StoredProcedure ? AddSquareBracket(name): name ;
            this.Parameters = parameters;
            this.ReplaceParameters = replaceParameters;
            FieldSource = fieldSource;
            DBOperationResultType = dBOperationResultType;
            this.ResultFields = fields.Count(f => f.Name != null) == 0 ? null : fields;
            if(setting.IgnoreFields != null && this.ResultFields != null)
            {
                this.ResultFields = this.ResultFields.Where(s => !setting.IgnoreFields!.Contains(s.Name, StringComparer.InvariantCultureIgnoreCase)).ToArray();
            }
            this.MethodName = methodName;
            this.ResultType = fields.Count(f => f.Name != null) == 0 ? new Identifier("", "Dictionary<string, object?>") : resultType;
            this.ParameterTypeName = parameters.Count == 0 ? null : parameterTypeName;
            this.Settings = setting;
        }


        private static string AddSquareBracket(DBObjectName name)
        {
            return "["+name.Schema+"].["+name.Name+"]";
        }
    }

}
