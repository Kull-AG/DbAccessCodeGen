using Kull.DatabaseMetadata;
using System;
using System.Data;
using System.Linq;

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

        public string CompleteNetType => IsTableValued ? "IEnumerable<" + NetType  + ">" + (IsNullable ? "?" : "") :
                 NetType + (IsNullable ? "?" : "");

        public bool IsTableValued { get; }

        public string TableValuedMeta { get; }
        public string TableValuedFn { get; }
        public ModelProperty(string sqlName, 
            string csName, 
            string parameterName, 
            string netType, 
            bool nullable,
            string getCode,
            ParameterDirection? parameterDirection,
            Model? userDefinedTableType)
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
            this.IsTableValued = userDefinedTableType != null;
            if(userDefinedTableType != null)
            {
                this.TableValuedMeta =
                    "new (string, Type)[] {" + string.Join(", ", userDefinedTableType.Properties.Select(s => "(" + "\"" + s.SqlName + "\", typeof(" + s.NetType + "))")) + "}";
                this.TableValuedFn = "row => new object[] {" + string.Join(", ", userDefinedTableType.Properties.Select(s => "row." + s.CSPropertyName)) + "}";
            } 
        }
    }

}