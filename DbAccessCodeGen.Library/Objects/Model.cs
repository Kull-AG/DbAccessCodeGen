using DbAccessCodeGen.Configuration;
using System.Collections.Generic;

namespace DbAccessCodeGen.Objects
{
    public class Model
    {
        public Identifier Name { get; }
        public IReadOnlyCollection<ModelProperty> Properties { get; } 


        public GeneratedCodeType CodeType { get; }

        public Model (Identifier name, IReadOnlyCollection<ModelProperty> properties, GeneratedCodeType codeType)
        {
            Name = name;
            this.Properties = properties;
            this.CodeType = codeType;
        }
    }
}