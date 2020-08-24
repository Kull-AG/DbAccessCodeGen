using DbAccessCodeGen.Objects;
using Jint;
using Kull.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbAccessCodeGen.Configuration
{
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

        private T? InvokeExternal<T>(string functionName, params object[] args)
            where T : class
        {
            if (engine == null) return default(T);
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
