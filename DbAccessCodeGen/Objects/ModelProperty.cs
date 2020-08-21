using Kull.DatabaseMetadata;
using System;
using System.Data;

namespace DbAccessCodeGen.Objects
{
    public class ModelProperty
    {
        public string SqlName { get; }

        public string CSPropertyName { get; }

        public string NetType { get; }

        public bool IsNullable { get; }
        public string GetCode { get; }
        public ParameterDirection? ParameterDirection { get; }
        public string ParameterName { get; }

        public string CompleteNetType => NetType + (IsNullable ? "?" : "");
        public ModelProperty(string sqlName, 
            string csName, 
            string parameterName, 
            string netType, 
            bool nullable,
            string getCode,
            ParameterDirection? parameterDirection)
        {
            this.SqlName = sqlName;
            this.CSPropertyName = csName;
            if (string.IsNullOrEmpty(csName))
                throw new ArgumentNullException(csName);
            this.NetType = netType;
            this.IsNullable = nullable;
            GetCode = getCode;
            ParameterDirection = parameterDirection;
            this.ParameterName = parameterName;
        }
    }

}