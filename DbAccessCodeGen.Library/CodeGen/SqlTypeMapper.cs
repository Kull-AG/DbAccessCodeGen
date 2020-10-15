using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DbAccessCodeGen.CodeGen
{
    public class SqlTypeMapper
    {
        protected Dictionary<string, string> MappingTypeMethod = new Dictionary<string, string>()
        {
            {"bool", nameof(IDataRecord.GetBoolean) },
            {"byte", nameof(IDataRecord.GetByte) },
            {"char", nameof(IDataRecord.GetChar) },
            {"DateTime", nameof(IDataRecord.GetDateTime) },
            {"decimal", nameof(IDataRecord.GetDecimal) },
            {"double", nameof(IDataRecord.GetDouble) },
            {"float", nameof(IDataRecord.GetFloat) },
            {"Guid", nameof(IDataRecord.GetGuid) },
            {"short", nameof(IDataRecord.GetInt16) },
            {"int", nameof(IDataRecord.GetInt32) },
            {"long", nameof(IDataRecord.GetInt64) },
            {"string", nameof(IDataRecord.GetString) }
        };

        public virtual string GetMappingCode(string netType, bool nullable, string name)
        {
            string ordinalName = "ordinals." + name;
            string recordVarName = "row";
            string baseTemplate;
            if (MappingTypeMethod.TryGetValue(netType, out var method))
            {
                baseTemplate = $"{recordVarName}.{method}({ordinalName})";
            }
            else
            {
                baseTemplate = $"({netType}){recordVarName}.GetValue({ordinalName})";
            }
            if (!nullable)
            {
                return $"{recordVarName}.{nameof(IDataRecord.IsDBNull)}({ordinalName}) ? throw new NullReferenceException(\"{name}\") : {baseTemplate}";
            }
            else
            {
                return $"{recordVarName}.{nameof(IDataRecord.IsDBNull)}({ordinalName}) ? ({netType}?)null : {baseTemplate}";
            }
        }
    }
}
