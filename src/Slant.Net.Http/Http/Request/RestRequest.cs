using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Slant.Net.Http.Response;
using Slant.Net.Http.Serialization;

namespace Slant.Net.Http.Request
{
    /// <summary>
    /// Default rest request implementation
    /// </summary>
    public class RestRequest : IRestRequest
    {
        protected RestRequest(HttpMethod method, string contentType)
        {
            Method = method;
            ContentType = contentType;

            CreateMessage = DefaultCreateRequestMessage;
            ReadMessageAsync = DefaultReadMessageAsync;

            Timeout = TimeSpan.Zero;
            ValidateMessageAsync = DefaultResponseValidator(this);
            ReadResponseBodyAsync = DefaultReadResponseBodyAsync;
        }

        public RestRequest(HttpMethod method)
            : this(method, null)
        {
            
        }

        public HttpMethod Method { get; set; }

        public string ContentType { get; set; }

        /// <summary>
        /// Absolute or relative path aka "objects/list"
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Custom timeout (using restclient timeout overwise)
        /// </summary>
        public TimeSpan Timeout { get; set; }

        public Func<HttpRequestMessage> CreateMessage { get; set; }

        /// <summary>
        /// Read response method
        /// </summary>
        public Func<HttpResponseMessage, Task<IRestResponse>> ReadMessageAsync { get; set; }

        /// <summary>
        /// Validate response method
        /// </summary>
        public Func<HttpResponseMessage, Task<bool>> ValidateMessageAsync { get; set; }


        public Func<HttpResponseMessage, Task<string>> ReadResponseBodyAsync { get; set; }

        #region [ Default request processing ]

        protected HttpRequestMessage DefaultCreateRequestMessage()
        {
            return new HttpRequestMessage(Method, Path);
        }

        protected HttpRequestMessage DefaultCreateRequestMessage(string content)
        {
            return DefaultCreateRequestMessage(this.Method.Method == HttpMethod.Get.Method ? null : StringContent(content));
        }

        protected HttpRequestMessage DefaultCreateRequestMessage(HttpContent content)
        {
            var requestMessage = new HttpRequestMessage(Method, Path);
            if (content != null)
            {
                requestMessage.Content = content;
            }
            return requestMessage;
        }

        public static Func<HttpResponseMessage, Task<bool>> DefaultResponseValidator(IRestRequest request)
        {
            return async message =>
            {
                if (message.IsSuccessStatusCode)
                {
                    return true;
                }
                string content = string.Empty;
                if (message.Content != null)
                {
                    content = await message.Content.ReadAsStringAsync();
                    message.Content.Dispose();
                }
                throw new RestRequestException(message.StatusCode, message.ReasonPhrase, request, content);
            };
        }

        public async Task<IRestResponse> DefaultReadMessageAsync(HttpResponseMessage message)
        {
            // Message validation
            // by default throw exception if not success request to API
            await ValidateMessageAsync(message);

            var response = new RestResponse(this)
            {
                Content = message.Content
            };
            response.Body = await ReadResponseBodyAsync(message);
            return response;
        }

        /// <summary>
        /// Read entire response stream to the string by default
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task<string> DefaultReadResponseBodyAsync(HttpResponseMessage message)
        {
            return message.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Read and deserialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="converter"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<IRestResponse<T>> DefaultReadDataMessageAsync<T>(HttpResponseMessage message, IObjectConverter converter)
            where T : class
        {
            // Message validation
            // by default throw exception if not success request to API
            await ValidateMessageAsync(message);

            var response = new RestResponse<T>(this)
            {
                Content = message.Content
            };
            response.Body = await ReadResponseBodyAsync(message);
            try
            {
                response.Data = converter?.Deserializer?.Deserialize<T>(response);
            }
            catch (Exception e)
            {
                e.Data["Body"] = response.Body;
                throw;
            }
            return response;
        }

        #endregion

        public HttpContent StringContent(string content)
        {
            return new StringContent(content, Encoding.UTF8, ContentType);
        }
    }

    /// <summary>
    /// Rest request with response deserialization support
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RestRequest<T> : RestRequest, IRestRequest<T> where T : class
    {
        protected RestRequest(HttpMethod method, string contentType) 
            : base(method, contentType)
        {
            ReadDataMessageAsync = DefaultReadDataMessageAsync<T>;
        }

        public RestRequest(HttpMethod method) 
            : this(method, null)
        {
        }

        public Func<HttpResponseMessage, IObjectConverter, Task<IRestResponse<T>>> ReadDataMessageAsync { get; set; }
    }

    public class RestRawRequest : RestRequest
    {
        public RestRawRequest(HttpMethod method) : base(method, null)
        {
            ReadMessageAsync = ReadRawMessageAsync;
        }

        public byte[] RawResponse { get; private set; }

        public async Task<IRestResponse> ReadRawMessageAsync(HttpResponseMessage message)
        {
            var resp = new RestResponse(this)
            {
                Content = message.Content
            };
            RawResponse = await message.Content.ReadAsByteArrayAsync();
            return resp;
        }
    }
}