using System;
using System.Net.Http;
using Slant.Net.Http.Serialization;
using Slant.Net.Http;

namespace Slant.Net.Http
{
    public class JsonRestClient : RestClient
    {
        public JsonRestClient(Func<HttpMessageHandler> httpHandler, ISerializer serializer, IDeserializer deserializer)
            : base(httpHandler)
        {
            Serializer = serializer ?? new JsonSerializer();
            Deserializer = deserializer ?? new JsonDeserializer();
        }

        public JsonRestClient()
            : this(null, null, null)
        {

        }
    }
}