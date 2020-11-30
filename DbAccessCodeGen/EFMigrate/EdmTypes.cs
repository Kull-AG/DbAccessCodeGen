using System;
using System.Collections.Generic;
using System.Text;

namespace DbAccessCodeGen.EFMigrate
{
    public class EdmTypes
    {
        Dictionary<string, string> edmToSqlMap = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
        {
            {"String", "nvarchar" },
            {"Bool", "bit" },
            {"Boolean", "bit" },
            {"Int16", "smallint" },
            {"Int32", "int" },
            {"Int64", "bigint" },
            {"Double", "float" },
            {"Float", "float" },
            {"Single", "real" },
            {"Decimal", "money" },
            {"Guid", "uniqueidentifier" },
            {"System.Byte[]", "varbinary" },
            {"Byte[]", "varbinary" },
            {"Byte", "byte" },
            {"Binary", "varbinary" },
            {"DateTime", "datetime" },
            {"Date", "date" },
            {"Time", "time" },
            {"TimeOfDay", "time" },
            {"DateTimeOffset", "datetimeoffset" },
        };


        public virtual string GetSqlType(string clrType)
        {
            if (edmToSqlMap.TryGetValue(clrType, out var vl))
                return vl;
            else
            {
                Console.Error.WriteLine($"Cannot map {clrType}");
                return clrType;
            }
        }

    }
}
