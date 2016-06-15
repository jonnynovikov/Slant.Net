using System;
using System.Net;
using System.Text;
using Slant.Net.Http.Response;
using Slant.Net.Http;

namespace Slant.Net.Http
{
    /// <summary>
    /// Error event arguments
    /// </summary>
    public class RestClientErrorEventArgs
    {
        public const HttpStatusCode HttpStatusNone = RestClient.HttpStatusNone;

        public RestClientErrorEventArgs()
        {
            HttpStatusCode = HttpStatusNone;
            ResponseStatus = RestResponseStatus.None;
        }

        public HttpStatusCode HttpStatusCode { get; set; }

        public RestResponseStatus ResponseStatus { get; set; }

        public Exception InnerException { get; set; }

        public string Message { get; set; }

        public bool IsRequestCompleted => HttpStatusCode != HttpStatusNone;

        public bool HasException => InnerException != null;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Status: {ResponseStatus}");
            if (IsRequestCompleted)
            {
                sb.Append($", HttpStatus: {HttpStatusCode}");
            }
            if (!string.IsNullOrEmpty(Message))
            {
                sb.Append($", Message: {Message}");
            }
            if (HasException)
            {
                sb.Append($", {Environment.NewLine}{InnerException}");
            }
            return sb.ToString();
        }
    }
}