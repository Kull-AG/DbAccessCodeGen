using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbAccessCodeGen.Configuration
{
    internal static class SettingsHelper
    {
        private static bool HandleSpecialCases<T>(object vl, out T? output)
        {
            if (typeof(T) == typeof(IReadOnlyDictionary<string, string>))
            {
                if (vl is IReadOnlyDictionary<string, object> orr)
                {
                    output=(T)(object)orr.ToDictionary(s => s.Key, o => (string)Convert.ChangeType(o, typeof(string)), StringComparer.OrdinalIgnoreCase);
                    return true;
                }
                if (vl is IReadOnlyDictionary<object, object> orr2)
                {
                    output = (T)(object)orr2.ToDictionary(s => (string)Convert.ChangeType(s.Key, typeof(string)), o => (string)Convert.ChangeType(o.Value, typeof(string)), StringComparer.OrdinalIgnoreCase);
                    return true;
                }
            }
            if (typeof(T) == typeof(IReadOnlyCollection<string>))
            {
                if (vl is IReadOnlyCollection<object> o)
                {
                    output = (T)(object)o.Select(o => (string)Convert.ChangeType(o, typeof(string))).ToArray();
                    return true;
                }
            }
            output = default(T);
            return false;
        }

        public static T? GetOrThrow<T>(this IReadOnlyDictionary<string, object> col, string key)
        {
            if (col.TryGetValue(key, out var vl))
            {
                if (vl is T t)
                    return t;
                if (vl == (object?)default(T))
                {
                    return default(T);
                }
                if (HandleSpecialCases<T>(vl, out var res)) return res;
                return (T)Convert.ChangeType(vl, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
            }
            throw new ArgumentException($"vl missing for key {key}");
        }

        public static T? GetOrThrow<T>(this IReadOnlyDictionary<string, object> col, string key, T defaultIfMissing)
        {
            if (col.TryGetValue(key, out var vl))
            {
                if (vl is T t)
                    return t;
                if (vl == (object?)default(T))
                {
                    return default(T);
                }
                if (HandleSpecialCases<T>(vl, out var res)) return res;
                return (T)Convert.ChangeType(vl, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
            }
            return defaultIfMissing;
        }
    }
}
