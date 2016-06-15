using System.Globalization;
using Newtonsoft.Json;

namespace Slant.Net.Http.Serialization
{
    public class JsonEncoder
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

        public static string Indented(object value)
        {
            return EncodeObject(value, DumpSerializerSettings);
        }
    }
}