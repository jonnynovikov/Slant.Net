using System.Net;
using System.Net.Http;
using Slant.Net.Http.Request;

namespace Slant.Net.Http
{
    public class RestRequestException : HttpRequestException
    {
        public RestRequestException(HttpStatusCode code, string reasonPhase, IRestRequest request, string content)
            : base($"Response status code does not indicate success: {(int)code} ({code.ToString()}) {reasonPhase}")
        {
            Request = request;
            StatusCode = code;
            ReasonPhrase = reasonPhase;
            Content = content ?? string.Empty;
        }

        public IRestRequest Request { get; private set; }

        public HttpStatusCode StatusCode { get; private set; }

        public string ReasonPhrase { get; private set; }

        public string Content { get; internal set; }
    }
}