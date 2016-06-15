using System;
using System.Net.Http;
using System.Threading.Tasks;
using Slant.Net.Http.Response;
using Slant.Net.Http.Serialization;

namespace Slant.Net.Http.Request
{
    /// <summary>
    /// Service request interface
    /// </summary>
    public interface IRestRequest
    {
        HttpMethod Method { get; set; }

        string ContentType { get; set; }

        string Path { get; set; }

        TimeSpan Timeout { get; set; }

        Func<HttpRequestMessage> CreateMessage { get; set; } 

        Func<HttpResponseMessage, Task<IRestResponse>> ReadMessageAsync { get; set; }
    }

    /// <summary>
    /// Service request interface which expect response serializable to specific type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRestRequest<T> : IRestRequest where T : class
    {
        Func<HttpResponseMessage, IObjectConverter, Task<IRestResponse<T>>> ReadDataMessageAsync { get; set; }
    }
}