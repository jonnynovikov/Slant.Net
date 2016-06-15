using System;
using System.Net.Http;
using Slant.Net.Http.Serialization;

namespace Slant.Net.Http
{
    public class XmlRestClient : RestClient
    {
        public XmlRestClient(Func<HttpMessageHandler> httpHandler, ISerializer serializer, IDeserializer deserializer)
            : base(httpHandler)
        {
            Serializer = serializer ?? XmlRequestSerializer.Shared;
            Deserializer = deserializer ?? XmlDeserializer.Shared;
        }

        public XmlRestClient()
            : this(null, null, null)
        {

        }
    }
}