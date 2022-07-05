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

        public string? TableValuedMeta { get; }
        public string? TableValuedFn { get; }

        public string? DefaultIfRequired { get; }
        public int? SizeIfRequired { get; }
        public ModelProperty(string sqlName, 
            string csName, 
            string parameterName, 
            string netType, 
            bool nullable,
            string getCode,
            ParameterDirection? parameterDirection,
            Model? userDefinedTableType,
            int? size,
            SqlType sqlType)
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
            if(this.ParameterDirection == System.Data.ParameterDirection.InputOutput)
            {
                if (sqlType.JsType == "string")
                {
                    DefaultIfRequired = "\"\"";
                }
                if (sqlType.NetType == typeof(byte[]))
                {
                    DefaultIfRequired = "Array.Empty<byte>()";
                }
                else if (sqlType.JsType == "number" || sqlType.JsType == "integer" || sqlType.JsType == "float")
                {
                    DefaultIfRequired = "0";
                }
                else if (sqlType.JsType == "boolean")
                {
                    DefaultIfRequired = "false";
                }
                else
                {
                    DefaultIfRequired = "default(" + sqlType.NetType + ")";
                }
                if (sqlType.DbType == "nvarchar" || sqlType.DbType == "ntext" || sqlType.DbType == "nchar") {
                    this.SizeIfRequired = size < 0 ? size: size * 2;
                }
                else
                    this.SizeIfRequired = size;
            }
            if(userDefinedTableType != null)
            {
                this.TableValuedMeta =
                    "new (string, Type)[] {" + string.Join(", ", userDefinedTableType.Properties.Select(s => "(" + "\"" + s.SqlName + "\", typeof(" + s.NetType + "))")) + "}";
                this.TableValuedFn = "row => new object?[] {" + string.Join(", ", userDefinedTableType.Properties.Select(s => "row." + s.CSPropertyName)) + "}";
            } 
        }
    }

}