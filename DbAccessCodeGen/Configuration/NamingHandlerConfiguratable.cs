using DbAccessCodeGen.Objects;
using Jint;
using Kull.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbAccessCodeGen.Configuration
{
    /// <summary>
    /// Allows for overwriting naming conventions by using JavaScript
    /// </summary>
    public class NamingHandlerConfiguratable : NamingHandler
    {
        Jint.Engine? engine;
        public NamingHandlerConfiguratable(Settings settings) : base(settings)
        {
            if (!string.IsNullOrEmpty(settings.NamingJS))
            {
                var namingJSFile = System.IO.File.ReadAllText(settings.NamingJS);
                engine = new Jint.Engine();
                engine.SetValue("Identifier", Jint.Runtime.Interop.TypeReference.CreateTypeReference(engine, typeof(Identifier)));
                engine.SetValue("DBObjectName", Jint.Runtime.Interop.TypeReference.CreateTypeReference(engine, typeof(DBObjectName)));
                engine.SetValue("NamingHandler", Jint.Runtime.Interop.TypeReference.CreateTypeReference(engine, typeof(NamingHandler)));
                engine.SetValue("Settings", Jint.Runtime.Interop.TypeReference.CreateTypeReference(engine, typeof(Settings)));
                engine.SetValue("def", new NamingHandler(settings));
                engine.SetValue("settings", settings);
                engine = engine.Execute(namingJSFile);
            }
        }

        public override Identifier GetParameterTypeName(DBObjectName name)
        {
            return InvokeExternal<Identifier>(nameof(GetParameterTypeName), name) ?? base.GetParameterTypeName(name);
        }

        public override Identifier GetIdentifierForUserDefinedType(DBObjectName type)
        {
            return InvokeExternal<Identifier>(nameof(GetIdentifierForUserDefinedType), type) ?? base.GetIdentifierForUserDefinedType(type);
        }

        public override string GetParameterName(string csName)
        {
            return InvokeExternal<string>(nameof(GetParameterName), csName) ?? base.GetParameterName(csName);
        }

        public override string GetPropertyName(string sqlName)
        {
            return InvokeExternal<string>(nameof(GetPropertyName), sqlName) ?? base.GetPropertyName(sqlName);
        }

        public override Identifier GetResultTypeName(DBObjectName name)
        {
            return InvokeExternal<Identifier>(nameof(GetResultTypeName), name) ?? base.GetResultTypeName(name);
        }

        public override string GetServiceClassMethodName(DBObjectName sp)
        {
            return InvokeExternal<string>(nameof(GetServiceClassMethodName), sp) ?? base.GetServiceClassMethodName(sp);
        }


        public override Identifier GetServiceClassName()
        {
            return InvokeExternal<Identifier>(nameof(GetServiceClassName)) ?? base.GetServiceClassName();
        }


        public override string MakeIdentifierValid(string identifier)
        {
            return InvokeExternal<string>(nameof(MakeIdentifierValid), identifier) ?? base.MakeIdentifierValid(identifier);
        }


        static object executionLock = new object();

        private T? InvokeExternal<T>(string functionName, params object[] args)
            where T : class
        {
            if (engine == null) return default(T);
            lock (executionLock)
            {
                var parameterTypeName = engine.GetValue(engine.Global, functionName);
                if (parameterTypeName.Type == Jint.Runtime.Types.Undefined)
                    return default(T);
                var res = parameterTypeName.Invoke(
                    args.Select(a => Jint.Native.JsValue.FromObject(engine, a)).ToArray());
                var ident = (T)res.ToObject();
                return ident;
            }
        }
    }
}
