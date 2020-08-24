using System;
using System.Collections.Generic;
using System.Text;

namespace DbAccessCodeGen.Configuration
{
    public class Settings
    {
        public string? ConnectionString { get; set; } 
        public string Namespace { get; set; } = "DbAccess";
        public string[] Procedures { get; set; } = new string[] { };

        public string OutputDir { get; set; } = "DbAccess";

        public bool GenerateAsyncCode { get; set; } = true;
        public bool GenerateSyncCode { get; set; } = false;

        public string? TemplateDir { get; set; }

        /// <summary>
        /// Path to a javascript file containing naming convention.
        /// </summary>
        public string? NamingJS { get; set; }
    }
}
