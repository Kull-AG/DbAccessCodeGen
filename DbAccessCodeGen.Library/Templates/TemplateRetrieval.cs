using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbAccessCodeGen.Templates
{
    public static class TemplateRetrieval
    {
        public static string GetTemplate(string name)
        {
            if (!name.EndsWith(".scriban")) return GetTemplate(name + ".scriban");
            string[] names = typeof(TemplateRetrieval).Assembly.GetManifestResourceNames();
            using (var stream = typeof(TemplateRetrieval).Assembly.GetManifestResourceStream(names.Single(n=>n.EndsWith("." + name)))
                ?? throw new ArgumentException("name not valid"))
            {
                using var strReader = new System.IO.StreamReader(stream);
                return strReader.ReadToEnd();
            }
        }
    }
}
