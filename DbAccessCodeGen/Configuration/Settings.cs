using System;
using System.Collections.Generic;
using System.Text;

namespace DbAccessCodeGen.Configuration
{
    public class Settings
    {
        public string ConnectionString { get; set; }
        public string Namespace { get; set; }
        public string[] Procedures { get; set; }

        public string OutputDir { get; set; }

        public bool GenerateAsyncCode { get; set; } = true;
        public bool GenerateSyncCode { get; set; } = false;
    }
}
