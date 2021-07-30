using DbAccessCodeGen.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DbAccessCodeGen.CodeGen
{
    public class SqlTypeMapper
    {
        private readonly Settings settings;
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
            {"string", nameof(IDataRecord.GetString) },
            {"object", nameof(IDataRecord.GetValue) }
        };

        public SqlTypeMapper(Configuration.Settings settings)
        {
            this.settings = settings;
        }

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
            if (settings.AlwaysAllowNullForStrings && (netType == "string" || netType == "System.String") && !nullable)
            {
                // Prefer not to make a runtime error here
                return $"{recordVarName}.{nameof(IDataRecord.IsDBNull)}({ordinalName}) ? null! : {baseTemplate}";
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
