using System.Collections.Generic;

namespace DbAccessCodeGen.Objects
{
    public class Model
    {
        public Identifier Name { get; }
        public IReadOnlyCollection<ModelProperty> Properties { get; } 

        public bool AllowSet { get; set; }

        public Model (Identifier name, IReadOnlyCollection<ModelProperty> properties)
        {
            Name = name;
            this.Properties = properties;
        }
    }
}