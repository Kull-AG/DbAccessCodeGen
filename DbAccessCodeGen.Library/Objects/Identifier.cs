using System;
using System.Collections.Generic;
using System.Text;

namespace DbAccessCodeGen.Objects
{
    public class Identifier
    {
        public string? Namespace { get; }
        public string Name { get; }

        public Identifier(string? @namespace, string name){
            this.Namespace = @namespace;
            this.Name = name;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Namespace)) return Name;
            return Namespace + "." + this.Name;
        }
    }
}
