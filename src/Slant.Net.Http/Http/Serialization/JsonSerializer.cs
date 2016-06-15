using Slant.Net.Http;

namespace Slant.Net.Http.Serialization
{
    public class JsonSerializer : ISerializer
    {
        private static ISerializer _shared;

        public static ISerializer Shared
        {
            get
            {
                if (_shared == null)
                {
                    return _shared = new JsonSerializer();
                }
                return _shared;
            }
        }

        public JsonConverter Converter { get; set; } = new JsonConverter();

        public JsonSerializer()
        {
            ContentType = HttpContentTypes.ApplicationJson;
        }

        public string Serialize(object obj)
        {
            return Converter.Encode(obj);
        }

        public string ContentType { get; set; }
    }
}