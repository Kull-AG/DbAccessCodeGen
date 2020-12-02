﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbAccessCodeGen.Configuration
{
    internal static class SettingsHelper
    {
        public static T GetOrThrow<T>(this IReadOnlyDictionary<string, object> col, string key)
        {
            if (col.TryGetValue(key, out var vl))
            {
                if (vl is T t)
                    return t;
                if (vl == (object)default(T))
                {
                    return default(T);
                }
                return (T)Convert.ChangeType(vl, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
            }
            throw new ArgumentException($"vl missing for key {key}");
        }

        public static T GetOrThrow<T>(this IReadOnlyDictionary<string, object> col, string key, T defaultIfMissing)
        {
            if(col.TryGetValue(key, out var vl))
            {
                if (vl is T t)
                    return t;
                if (vl == (object)default(T))
                {
                    return default(T);
                }
                return (T)Convert.ChangeType(vl, Nullable.GetUnderlyingType(typeof(T))?? typeof(T));
            }
            return defaultIfMissing;
        }
    }
}
