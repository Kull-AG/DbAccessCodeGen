using System;
using System.Collections.Generic;
using System.Text;

namespace DbAccessCodeGen.EFMigrate
{

    internal readonly struct ComplexTypeInfo
    {
        public string Type { get; }
        public string Name { get; }
        public bool Nullable { get; }
        public int MaxLength { get; }
        public ComplexTypeInfo(string type, string name, bool nullable, int maxlength)
        {
            this.Type = type;
            this.Name = name;
            this.Nullable = nullable;
            this.MaxLength = maxlength;
        }
    }
}
