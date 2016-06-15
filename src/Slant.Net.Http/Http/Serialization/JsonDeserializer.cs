using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Slant.Net.Http.Response;

namespace Slant.Net.Http.Serialization
{
    public class JsonDeserializer : IDeserializer
    {
        public JsonConverter Converter { get; set; } = new JsonConverter();

        private static IDeserializer _shared;

        public static IDeserializer Shared
        {
            get
            {
                if (_shared == null)
                {
                    return _shared = new JsonDeserializer();
                }
                return _shared;
            }
        }
        
        public T Deserialize<T>(IRestResponse response) where T : class
        {
            return Converter.Decode<T>(response.Body);
        }

        public T DeserializeXml<T>(string content) where T : class
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);

            var json = Regex.Replace(
                    JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.None, true),
                     "(?<=(\\,\\\"|\\{\\\"))(@)(?!.*\\\":\\\\s )", string.Empty, RegexOptions.IgnoreCase);

            return Converter.Decode<T>(json);
        }
    }
}