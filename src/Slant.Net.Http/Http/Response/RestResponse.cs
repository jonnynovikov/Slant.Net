using System.Net.Http;
using System.Threading.Tasks;
using Slant.Net.Http.Request;
using Slant.Net.Http.Serialization;

namespace Slant.Net.Http.Response
{

    public class RestResponse : IRestResponse
    {
        public RestResponse(IRestRequest request)
        {
            Request = request;
        }

        private string _cachedBody;

        public string Body
        {
            get
            {
                if (!string.IsNullOrEmpty(_cachedBody))
                {
                    return _cachedBody;
                }
                if (Content != null)
                {
                    _cachedBody = Content.ReadAsStringAsync().Result;
                    return _cachedBody;
                }
                return null;
            }
            set { _cachedBody = value; }
        }

        public HttpContent Content { get; set; }

        public IRestRequest Request { get; set; }
    }

    public class RestResponse<T> : RestResponse, IRestResponse<T> where T : class
    {
        public RestResponse(IRestRequest request) 
            : base(request)
        {
            
        }

        public T Data { get; set; }
    }

}