using System;
using System.Net.Http;
using System.Threading.Tasks;
using Slant.Net.Http.Request;

namespace Slant.Net.Http.Response
{
    /// <summary>
    /// Status for responses
    /// </summary>
    public enum RestResponseStatus
    {
        None,
        Completed,
        Error,
        TimedOut,
        Aborted
    }

    public interface IRestResponse
    {
        string Body { get; set; }

        HttpContent Content { get; }

        IRestRequest Request { get; set; }
    }

    public interface IRestResponse<T> : IRestResponse 
        where T : class 
    {
        T Data { get; set; }
    }
}