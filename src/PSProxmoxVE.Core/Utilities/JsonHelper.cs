using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PSProxmoxVE.Core.Utilities
{
    /// <summary>
    /// Provides helper methods for converting Newtonsoft.Json types to native .NET types.
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Converts a JToken to a native .NET type recursively.
        /// JObject becomes Dictionary&lt;string, object&gt;, JArray becomes List&lt;object&gt;, JValue becomes primitive.
        /// </summary>
        public static object? ToNative(JToken? token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            switch (token)
            {
                case JObject obj:
                    return obj.Properties().ToDictionary(
                        p => p.Name,
                        p => ToNative(p.Value));

                case JArray arr:
                    return arr.Select(ToNative).ToList();

                case JValue val:
                    return val.Value;

                default:
                    return token.ToString();
            }
        }

        /// <summary>
        /// Converts a JObject to a Dictionary&lt;string, object?&gt;.
        /// </summary>
        public static Dictionary<string, object?> ToDictionary(JObject? obj)
        {
            if (obj == null) return new Dictionary<string, object?>();
            return obj.Properties().ToDictionary(
                p => p.Name,
                p => ToNative(p.Value));
        }

        /// <summary>
        /// Converts a JArray to a List of Dictionaries.
        /// Each element should be a JObject; non-object elements are skipped.
        /// </summary>
        public static List<Dictionary<string, object?>> ToListOfDictionaries(JArray? arr)
        {
            if (arr == null) return new List<Dictionary<string, object?>>();
            return arr.OfType<JObject>().Select(ToDictionary).ToList();
        }
    }
}
