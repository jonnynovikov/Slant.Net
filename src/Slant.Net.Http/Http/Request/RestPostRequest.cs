using System;
using System.Net.Http;
using System.Threading.Tasks;
using Slant.Net.Http.Response;
using Slant.Net.Http.Serialization;

namespace Slant.Net.Http.Request
{
    /// <summary>
    /// Simple POST request with HttpContent
    /// </summary>
    public class RestPostRequest : RestRequest
    {
        private HttpContent Body { get; set; }

        public RestPostRequest(string path, HttpContent content)
            : base(HttpMethod.Post)
        {
            Path = path;
            Body = content;

            CreateMessage = () => DefaultCreateRequestMessage(Body);
            ContentType = content.Headers.ContentType?.MediaType;
        }
    }

    public class RestPostRequest<T> : RestPostRequest, IRestRequest<T> where T : class
    {
        public RestPostRequest(string path, HttpContent content) : base(path, content)
        {
            ReadDataMessageAsync = this.DefaultReadDataMessageAsync<T>;
        }

        public Func<HttpResponseMessage, IObjectConverter, Task<IRestResponse<T>>> ReadDataMessageAsync { get; set; }
    }
}