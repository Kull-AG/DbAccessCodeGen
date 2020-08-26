﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml;

namespace DbAccessCodeGen.MigrateEF
{
    public class Program
    {
        static Dictionary<string, string> clrToSqlMap = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
        {
            {"String", "nvarchar" },
            {"Bool", "bit" },
            {"Boolean", "bit" },
            {"Int16", "smallint" },
            {"Int32", "int" },
            {"Int64", "long" },
            {"Double", "float" },
            {"Float", "float" },
            {"Single", "float" },
            {"Decimal", "float" },
            {"Guid", "uniqueidentifier" },
            {"System.Byte[]", "varbinary" },
            {"Binary", "varbinary" },
            {"DateTime", "datetime" },
            {"DateTimeOffset", "datetimeoffset" },
        };

        readonly struct ComplexTypeInfo
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
        static void Main(string[] args)
        {
            string efFile = @"E:\Code\Kull\P1004\EmeraldVentures-GUI\DataAccess\EmeraldEntities.edmx";
            var outDir = "Output";
            
            string currentFunctionImportName = null;
            string currentComplexType = null;
            Dictionary<string, string> mapFunctionToReturnType = new Dictionary<string, string>();
            Dictionary<string, List<ComplexTypeInfo>> complexTypeFields = new Dictionary<string, List<ComplexTypeInfo>>();
            XmlReader xmlReader = XmlReader.Create(efFile);
            while (xmlReader.Read())
            {
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.LocalName == "FunctionImportMapping"))
                {
                    currentFunctionImportName = xmlReader.GetAttribute("FunctionImportName");
                }
                else if (xmlReader.NodeType == XmlNodeType.EndElement && (xmlReader.LocalName == "FunctionImportMapping"))
                {
                    currentFunctionImportName = null;
                }
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.LocalName == "ComplexTypeMapping"))
                {
                    string typeName = xmlReader.GetAttribute("TypeName");
                    if (!mapFunctionToReturnType.ContainsKey(typeName))
                        mapFunctionToReturnType.Add(currentFunctionImportName, typeName);
                }

                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.LocalName == "ComplexType"))
                {
                    currentComplexType = xmlReader.GetAttribute("Name");
                    complexTypeFields.Add(currentComplexType, new List<ComplexTypeInfo>());
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
                    var info = new ComplexTypeInfo(xmlReader.GetAttribute("Type"),
                        xmlReader.GetAttribute("Name"),
                        bool.Parse(xmlReader.GetAttribute("Nullable")),
                        string.IsNullOrEmpty(maxLength) ? 0 : int.Parse(maxLength));
                    curList.Add(info);
                }

            }
            foreach (var item in complexTypeFields)
            {
                string typeName = item.Key;
                var fields = item.Value;
                try
                {
                    var spName = (mapFunctionToReturnType.Select(kv => (KeyValuePair<string, string>?)kv).FirstOrDefault(r => r.Value.Value == typeName)
                        ?? mapFunctionToReturnType.First(r => r.Value.EndsWith("." + typeName, StringComparison.CurrentCultureIgnoreCase))).Key;
                    var jsonToPrint = fields.Select(f => new { Name = f.Name, TypeName = GetSqlType(f.Type), IsNullable = f.Nullable, f.MaxLength }).ToArray();
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
                    var filePath = System.IO.Path.Combine(resultDir, spName + ".json");
                    System.IO.File.WriteAllText(filePath, json);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error for {typeName}: \r\n{e}");
                }
            }
            var procs = mapFunctionToReturnType.Select(s => s.Key).ToArray();
            var procString = string.Join("\r\n", procs.Select(s => " - " + s).OrderBy(k=>k));
            var yaml =
@"---
OutputDir: SET_THIS
Namespace: SET_THIS
ConnectionString: SET_THIS
Procedures:
" + procString;
            System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "DbCodeGenConfig.yml"), yaml);
        }

        private static string GetSqlType(string clrType)
        {
            if (clrToSqlMap.TryGetValue(clrType, out var vl))
                return vl;
            else
            {
                Console.Error.WriteLine($"Cannot map {clrType}");
                return clrType;
            }
        }
    }
}
