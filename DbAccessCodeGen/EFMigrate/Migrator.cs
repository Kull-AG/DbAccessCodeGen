using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using System.Text.Json;
using Kull.Data;

namespace DbAccessCodeGen.EFMigrate
{
    class Migrator
    {
        private readonly EdmTypes edmTypes;

        public Migrator(EdmTypes edmTypes)
        {
            this.edmTypes = edmTypes;
        }

        public async Task Execute(string edmxFile, string outDir)
        {

            string? currentFunctionImportName = null;
            string? currentComplexType = null;
            Dictionary<string, string> mapFunctionToReturnType = new Dictionary<string, string>();
            Dictionary<string, List<ComplexTypeInfo>> complexTypeFields = new Dictionary<string, List<ComplexTypeInfo>>();
            HashSet<DBObjectName> sps = new HashSet<DBObjectName>();
            XmlReader xmlReader = XmlReader.Create(edmxFile);
            while (xmlReader.Read())
            {

                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.LocalName == "FunctionImport"))
                {
                    string spName = xmlReader.GetAttribute("Name")!;
                    sps.Add(spName);
                }
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.LocalName == "FunctionImportMapping"))
                {
                    currentFunctionImportName = xmlReader.GetAttribute("FunctionImportName");
                    sps.Add(currentFunctionImportName!);
                }
                else if (xmlReader.NodeType == XmlNodeType.EndElement && (xmlReader.LocalName == "FunctionImportMapping"))
                {
                    currentFunctionImportName = null;
                }
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.LocalName == "ComplexTypeMapping"))
                {
                    string typeName = xmlReader.GetAttribute("TypeName")!;
                    if (!mapFunctionToReturnType.ContainsKey(typeName))
                        mapFunctionToReturnType.Add(currentFunctionImportName ?? throw new NullReferenceException("currentFunctionImportName"), typeName);
                }

                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.LocalName == "ComplexType"))
                {
                    currentComplexType = xmlReader.GetAttribute("Name");
                    complexTypeFields.Add(currentComplexType!, new List<ComplexTypeInfo>());
                }
                if ((xmlReader.NodeType == XmlNodeType.EndElement) && (xmlReader.LocalName == "ComplexType"))
                {
                    currentComplexType = null;
                }
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.LocalName == "Property")
                        && currentComplexType != null)
                {
                    var curList = complexTypeFields[currentComplexType];
                    var maxLength = xmlReader.GetAttribute("MaxLength");
                    string? nullable = xmlReader.GetAttribute("Nullable");
                    var info = new ComplexTypeInfo(xmlReader.GetAttribute("Type")!,
                        xmlReader.GetAttribute("Name")!,
                         string.IsNullOrEmpty(nullable) ? true : bool.Parse(nullable),
                        string.IsNullOrEmpty(maxLength) || "MAX".Equals(maxLength, StringComparison.InvariantCultureIgnoreCase) ? 0 : int.Parse(maxLength));
                    curList.Add(info);
                }

            }
            foreach (var item in complexTypeFields)
            {
                string typeName = item.Key;
                var fields = item.Value;
                try
                {
                    var spName = (mapFunctionToReturnType.Select(kv => (KeyValuePair<string, string>?)kv).FirstOrDefault(r => r!.Value.Value == typeName)
                        ?? mapFunctionToReturnType.FirstOrDefault(r => r.Value.EndsWith("." + typeName, StringComparison.CurrentCultureIgnoreCase))).Key;
                    if (string.IsNullOrEmpty(spName))
                    {
                        Console.Error.WriteLine($"Type not mapped: {typeName}");
                    }
                    var jsonToPrint = fields.Select(f => new { Name = f.Name, TypeName = edmTypes.GetSqlType(f.Type), IsNullable = f.Nullable, f.MaxLength }).ToArray();
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                    };
                    var json = System.Text.Json.JsonSerializer.Serialize(jsonToPrint, options);
                    var resultDir = System.IO.Path.Combine(outDir, "ResultSets");
                    if (!System.IO.Directory.Exists(resultDir))
                    {
                        System.IO.Directory.CreateDirectory(resultDir);
                    }
                    var filePath = System.IO.Path.Combine(resultDir, (DBObjectName)spName + ".json");
                    await System.IO.File.WriteAllTextAsync(filePath, json);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error for {typeName}: \r\n{e}");
                }
            }
            var procs = sps.OrderBy(s=>s).Select(s=>s.ToString(false)).ToArray();
            var procString = string.Join("\r\n", procs.Select(s => " - " + s).OrderBy(k => k));
            var yaml =
@"---
OutputDir: SET_THIS
Namespace: SET_THIS
ConnectionString: SET_THIS
Procedures:
" + procString;
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(outDir, "DbCodeGenConfig.yml"), yaml);
        }
    }
}
