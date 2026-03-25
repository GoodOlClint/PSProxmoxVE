using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSProxmoxVE.Core.Utilities
{
    /// <summary>
    /// JSON converter that deserializes a JSON array into a List of Dictionary&lt;string, object?&gt;.
    /// Each array element is expected to be an object; non-object elements are skipped.
    /// </summary>
    public class NativeListConverter : JsonConverter<List<Dictionary<string, object?>>>
    {
        /// <inheritdoc />
        public override List<Dictionary<string, object?>> ReadJson(JsonReader reader, Type objectType, List<Dictionary<string, object?>>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return token is JArray arr ? JsonHelper.ToListOfDictionaries(arr) : new List<Dictionary<string, object?>>();
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, List<Dictionary<string, object?>>? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    /// <summary>
    /// JSON converter that deserializes a JSON object into a Dictionary&lt;string, object?&gt;.
    /// </summary>
    public class NativeDictionaryConverter : JsonConverter<Dictionary<string, object?>>
    {
        /// <inheritdoc />
        public override Dictionary<string, object?> ReadJson(JsonReader reader, Type objectType, Dictionary<string, object?>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return token is JObject obj ? JsonHelper.ToDictionary(obj) : new Dictionary<string, object?>();
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, Dictionary<string, object?>? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
