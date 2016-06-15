using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Slant.Net.Http.Response;
using Slant.Net.Http.Serialization;

namespace Slant.Net.Http.Request
{
    /// <summary>
    /// JSON service request
    /// </summary>
    public class JsonDataRequest<T> : RestRequest
    {
        public ISerializer Serializer { get; set; }

        public JsonDataRequest(HttpMethod method, string path)
            : base(method)
        {
            Path = path;
            ContentType = HttpContentTypes.ApplicationJson;

            CreateMessage = () => DefaultCreateRequestMessage(Body);
        }

        public JsonDataRequest(HttpMethod method, string path, T content)
            : this(method, path)
        {
            Content = content;
        }

        private T _content;

        public T Content
        {
            get { return _content; }
            set
            {
                _content = value;
                // Clear cache
                _cachedBody = string.Empty;
            }
        }

        /// <summary>
        /// Cached serialized request body
        /// </summary>
        private string _cachedBody;

        public string Body
        {
            get
            {
                if (!string.IsNullOrEmpty(_cachedBody))
                    return _cachedBody;

                if (Content == null || Serializer == null)
                    return string.Empty;

                _cachedBody = Serializer.Serialize(Content);
                return _cachedBody;
            }
            set { _cachedBody = value; }
        }

        //public static HttpContent JsonFormattedContent(JsonRequest request)
        //{
        //    var obj = request.Body;

        //    var jc = new ObjectContent(
        //        obj.GetType(),
        //        obj,
        //        new JsonMediaTypeFormatter());

        //    return jc;
        //}

        public HttpContent JsonSerializedContent()
        {
            var content = Serializer.Serialize(Content);
            return new StringContent(content, Encoding.UTF8, ContentType);
        }
    }

    public class JsonRequest : JsonDataRequest<object>
    {

        public JsonRequest(HttpMethod method, string path) : base(method, path)
        {
        }

        public JsonRequest(HttpMethod method, string path, object content) : base(method, path, content)
        {
        }
    }

    public class JsonRequest<T> : JsonRequest, IRestRequest<T> where T : class 
    {
        public JsonRequest(HttpMethod method) 
            : base(method, HttpContentTypes.ApplicationJson)
        {
            ReadDataMessageAsync = DefaultReadDataMessageAsync<T>;
        }

        public Func<HttpResponseMessage, IObjectConverter, Task<IRestResponse<T>>> ReadDataMessageAsync { get; set; }
    }

    public class JsonRequest<TSend, TReceive> : JsonDataRequest<TSend>, IRestRequest<TReceive> 
        where TReceive : class
    {

        public JsonRequest(HttpMethod method, string path) : base(method, path)
        {
            ReadDataMessageAsync = DefaultReadDataMessageAsync<TReceive>;
        }

        public JsonRequest(HttpMethod method, string path, TSend content) : base(method, path, content)
        {
        }

        public Func<HttpResponseMessage, IObjectConverter, Task<IRestResponse<TReceive>>> ReadDataMessageAsync { get; set; }
    }
}