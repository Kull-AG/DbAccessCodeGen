using DbAccessCodeGen.Objects;
using Kull.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbAccessCodeGen.Configuration
{
    public class NamingHandler
    {
        protected static readonly string[] csharpKeywords = new string[] { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while" };

        private readonly Settings settings;

        public NamingHandler(Configuration.Settings settings)
        {
            this.settings = settings;
        }

        public virtual Identifier GetIdentifierForUserDefinedType(DBObjectName type)
        {
            return new Identifier(settings.Namespace + ".UDT", GetCSName(type.Name));
        }

        public bool isCSharpKeyword(string name) => csharpKeywords.Contains(name);

        public virtual string GetParameterName(string csName)
        {
            string lowerCase = csName[0].ToString().ToLower() + csName.Substring(1);
            if (isCSharpKeyword(lowerCase))
                return "@" + lowerCase;
            return lowerCase;
        }

        public virtual string MakeIdentifierValid(string identifier)
        {
            return identifier.Replace(" ", "").Replace("-", "");
        }

        protected virtual string GetCSName(string sqlName)
        {
            var upperCase = sqlName[0].ToString().ToUpper() + sqlName.Substring(1);
            if (Microsoft.CodeAnalysis.CSharp.SyntaxFacts.IsValidIdentifier(upperCase))
                return upperCase;
            else
            {
                upperCase = MakeIdentifierValid(upperCase); 
                if (!Microsoft.CodeAnalysis.CSharp.SyntaxFacts.IsValidIdentifier(upperCase))
                    throw new ArgumentException("Cannot handle " + upperCase);
                return upperCase;
            }
        }

        public virtual Identifier GetParameterTypeName(DBObjectName name)
        {
            return new Identifier(settings.Namespace, GetCSName(name.Name) + "Parameters");
        }

        public virtual Identifier GetResultTypeName(DBObjectName name)
        {
            return new Identifier(settings.Namespace, GetCSName(name.Name) + "Result");
        }

        public virtual string GetPropertyName(string sqlName)
        {
            return GetCSName(sqlName);
        }

        public virtual string GetServiceClassMethodName(DBObjectName sp)
        {
            return sp.Name;
        }

        public virtual Identifier GetServiceClassName()
        {
            return new Identifier(settings.Namespace, "DataAccessor");
        }
    }
}
