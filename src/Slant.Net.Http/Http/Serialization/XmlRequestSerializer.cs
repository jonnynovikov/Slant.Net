using System.IO;
using System.Xml.Serialization;

namespace Slant.Net.Http.Serialization
{
    public class XmlRequestSerializer : ISerializer
    {
        private static ISerializer _shared;

        public static ISerializer Shared
        {
            get
            {
                if (_shared == null)
                {
                    return _shared = new XmlRequestSerializer();
                }
                return _shared;
            }
        }

        public XmlRequestSerializer()
        {
            ContentType = HttpContentTypes.TextXml;
        }

        public string Serialize(object obj)
        {
            var xs = new XmlSerializer(obj.GetType());
            using (var writer = new StringWriter())
            {
                xs.Serialize(writer, obj);
                return writer.ToString();
            }
        }

        public string ContentType { get; set; }
    }
}