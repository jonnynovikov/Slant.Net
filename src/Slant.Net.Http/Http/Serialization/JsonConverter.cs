using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace Slant.Net.Http.Serialization
{
    public class JsonConverter
    {
        private static readonly JsonSerializerSettings DumpSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Culture = CultureInfo.InvariantCulture,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            Culture = CultureInfo.InvariantCulture,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        public JsonSerializerSettings SerializerSettings { get; set; }

        private static readonly Dictionary<Type, JsonSerializerSettings> SerializerSettingsMap 
            = new Dictionary<Type, JsonSerializerSettings>(); 

        public static void SetSettings(Type t, JsonSerializerSettings settings)
        {
            SerializerSettingsMap[t] = settings;
        }

        public static JsonSerializerSettings GetSettings<T>()
        {
            JsonSerializerSettings settings;
            return SerializerSettingsMap.Count == 0
                ? DefaultSerializerSettings
                : (SerializerSettingsMap.TryGetValue(typeof (T), out settings) ? settings : DefaultSerializerSettings);
        }

        public JsonConverter()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                Culture = CultureInfo.InvariantCulture,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
        }

        public string Encode(object obj)
        {
            return EncodeObject(obj, SerializerSettings, Formatting.None);
        }

        public T Decode<T>(string content)
        {
            return JsonConvert.DeserializeObject<T>(content, SerializerSettings);
        }

        public static string EncodeObject(object obj)
        {
            return EncodeObject(obj, null);
        }

        public static string EncodeObject(object obj, JsonSerializerSettings settings, Formatting formatting = Formatting.None)
        {
            if (obj == null)
                return string.Empty;
            var s = obj as string;
            var encoded = s ?? JsonConvert.SerializeObject(obj, formatting, settings ?? DefaultSerializerSettings);
            return encoded;
        }

        public static string DumpObject(object value)
        {
            return JsonConvert.SerializeObject(value, DumpSerializerSettings);
        }
    }
}