using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Slant.Net.Http.Request;
using Slant.Net.Http.Response;
using Slant.Net.Http.Serialization;

namespace Slant.Net.Http
{
    /// <summary>
    /// Reusable клиент для запросов к REST API поставщиков
    /// По умолчанию он настроен для использования API,
    /// принимающий и отдающий JSON
    /// </summary>
    public partial class RestClient : IRestClient
    {
        #region [ Members ]

        public const HttpStatusCode HttpStatusNone = HttpStatusCode.Unused;

        public delegate void RestClientErrorHandler(object sender, RestClientErrorEventArgs e);

        public static Func<HttpMessageHandler> DefaultHttpHandlerFactory = () => new HttpClientHandler();

        public static Func<HttpMessageHandler> UnProxiedHttpHandlerFactory = () => new HttpClientHandler
        {
            UseProxy = false,
            Proxy = null
        };

        public static Func<HttpMessageHandler> GlobalHttpHandlerFactory;

        /// <summary>
        /// HTTP message handlers for this client
        /// </summary>
        private Lazy<HttpMessageHandler> _httpClientHandler;

        public bool AsyncContinueOnCapturedContext { get; set; }

        public ISerializer Serializer { get; set; }

        public IDeserializer Deserializer { get; set; }

        /// <summary>
        /// HttpClient BaseAddress
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Network timeout
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Message handler factory setter
        /// </summary>
        public Func<HttpMessageHandler> HttpMessageHandler
        {
            set
            {
                if (_httpClientHandler != null && _httpClientHandler.IsValueCreated)
                {
                    _httpClientHandler.Value.Dispose();
                }
                if (value == null)
                {
                    _httpClientHandler = new Lazy<HttpMessageHandler>(GlobalHttpHandlerFactory ?? DefaultHttpHandlerFactory);
                    return;
                }
                _httpClientHandler = new Lazy<HttpMessageHandler>(value);
            }
        }

        /// <summary>
        /// Error handler
        /// </summary>
        public event RestClientErrorHandler OnError;

        #endregion

        public RestClient(Func<HttpMessageHandler> handlerFactory)
        {
            HttpMessageHandler = handlerFactory;
            CreateHttpClient = CreateDefaultClient;
        }

        public RestClient()
            : this(null)
        {
            
        }

        #region [ IRestClient implementation ]

        public static Task<HttpResponseMessage> DefaultSendRequestAsync(IRestRequest request, HttpClient client, CancellationToken cancellationToken)
        {
            var message = request.CreateMessage();
            return client.SendAsync(message, cancellationToken);
        }

        public Func<IRestRequest, HttpClient, CancellationToken, Task<HttpResponseMessage>> SendRequestAsync = DefaultSendRequestAsync;
        
        /// <summary>
        /// Request processing with exception handling
        /// </summary>
        /// <typeparam name="TRestRequest"></typeparam>
        /// <typeparam name="TRestResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="messageHandler"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TRestResponse> MakeRequestAsync<TRestRequest, TRestResponse>(
            TRestRequest request, 
            Func<TRestRequest, HttpResponseMessage, Task<TRestResponse>> messageHandler,
            CancellationToken cancellationToken) 
                where TRestRequest  : IRestRequest
                where TRestResponse : IRestResponse
        {
            var status = RestResponseStatus.None;
            var httpStatus = HttpStatusNone;
            try
            {
                using (var httpClient = CreateHttpClient(request))
                {
                    var responseMessage =
                        await
                            SendRequestAsync(request, httpClient, cancellationToken)
                                .ConfigureAwait(AsyncContinueOnCapturedContext);
                    httpStatus = responseMessage.StatusCode;

                    status = responseMessage.IsSuccessStatusCode
                        ? RestResponseStatus.Completed
                        : RestResponseStatus.Error;

                    var response =
                        await messageHandler(request, responseMessage).ConfigureAwait(AsyncContinueOnCapturedContext);
                    return response;
                }
            }
            catch (HttpRequestException hre)
            {
                if (OnError == null) throw;
                status = status == RestResponseStatus.None ? RestResponseStatus.Aborted : RestResponseStatus.Error;
                HandleError(new RestClientErrorEventArgs()
                {
                    HttpStatusCode = httpStatus,
                    ResponseStatus = status,
                    InnerException = hre,
                    Message = "Network error"
                });
                throw;
            }
            catch (WebException we)
            {
                if (OnError == null) throw;
                HandleError(new RestClientErrorEventArgs()
                {
                    HttpStatusCode = httpStatus,
                    ResponseStatus = status,
                    InnerException = we,
                    Message = "Network protocol error"
                });
                throw;
            }
            catch (TaskCanceledException tce)
            {
                if (OnError == null) throw;
                status = RestResponseStatus.TimedOut;
                HandleError(new RestClientErrorEventArgs()
                {
                    HttpStatusCode = httpStatus,
                    ResponseStatus = status,
                    InnerException = tce,
                    Message = "Request cancelled by timeout"
                });
                throw;
            }
            catch (Exception ex)
            {
                if (OnError == null) throw;
                status = RestResponseStatus.Error;
                HandleError(new RestClientErrorEventArgs()
                {
                    HttpStatusCode = httpStatus,
                    ResponseStatus = status,
                    InnerException = ex,
                    Message = "General error. See inner exception"
                });
                throw;
            }
        }

        /// <summary>
        /// Send request with error handling
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IRestResponse> SendAsync(IRestRequest request, CancellationToken cancellationToken)
        {
            return MakeRequestAsync(request, 
                (req, message) => req.ReadMessageAsync(message), cancellationToken);
        }

        /// <summary>
        /// Send request with error handling
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IRestResponse<T>> SendAsync<T>(IRestRequest<T> request, CancellationToken cancellationToken) where T : class 
        {
            return MakeRequestAsync(request,
                (req, message) => req.ReadDataMessageAsync(message, this), cancellationToken);
        }

        internal void HandleError(RestClientErrorEventArgs args)
        {
            if (OnError != null)
            {
                OnError(this, args);
                return;
            }
            if (args.InnerException != null)
            {
                throw args.InnerException;
            }
        }
        
        #endregion

        #region [ HTTP specific ]

        protected virtual HttpMessageHandler MessageHandler => _httpClientHandler.Value;

        public HttpClient CreateDefaultClient(IRestRequest request)
        {
            HttpClient httpClient = new HttpClient(MessageHandler, false);
            if (!string.IsNullOrEmpty(BaseUrl))
            {
                httpClient.BaseAddress = new Uri(BaseUrl);
            }
            if (!request.Timeout.Equals(TimeSpan.Zero))
            {
                httpClient.Timeout = Timeout;
            }
            else
            {
                if (!this.Timeout.Equals(TimeSpan.Zero))
                {
                    httpClient.Timeout = this.Timeout;
                }
                else
                {
                    // timeout not defined
                }
            }
            
            if (!string.IsNullOrEmpty(request.ContentType))
            {
                // Add an Accept header for ContentType format.
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(request.ContentType));
            }

            // Expect: 100-continue Header Problem
            // Fixed in HttpClient setting the default request header flag
            // Without this flag requests are slow down
            // http://haacked.com/archive/2004/05/15/http-web-request-expect-100-continue.aspx/
            //
            // do not use this flag because of practical useless
            // httpClient.DefaultRequestHeaders.ExpectContinue = false;
            return httpClient;
        }

        public Func<IRestRequest, HttpClient> CreateHttpClient { get; set; } 

        public void Dispose()
        {
            if (_httpClientHandler?.Value != null)
            {
                _httpClientHandler.Value.Dispose();
                _httpClientHandler = null;
            }
        }

        #endregion

        #region [ Helpers ]

        public static string Md5Hash(string data)
        {
            var checkSum = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder();
            foreach (var b in checkSum)
            {
                sb.Append(b.ToString("x2").ToLower());
            }
            return sb.ToString();
        }

        #endregion
    }
}