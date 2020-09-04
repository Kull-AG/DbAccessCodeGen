using System;
using System.Collections.Generic;
using System.Text;

namespace DbAccessCodeGen.Library.Templates
{
    public static class TemplateRetrieval
    {
        public static string GetTemplate(string name)
        {
            if (!name.EndsWith(".scriban")) return GetTemplate(name + ".scriban");
            using (var stream = typeof(TemplateRetrieval).Assembly.GetManifestResourceStream("Templates/" + name)
                ?? throw new ArgumentException("name not valid"))
            {
                using var strReader = new System.IO.StreamReader(stream);
                return strReader.ReadToEnd();
            }
        }
    }
}
