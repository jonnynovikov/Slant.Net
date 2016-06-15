using System;
using System.Threading;
using System.Threading.Tasks;
using Slant.Net.Http.Request;
using Slant.Net.Http.Response;
using Slant.Net.Http.Serialization;

namespace Slant.Net.Http
{
    public interface IRestClient : IDisposable, IReplyClient, IObjectConverter
    {
        string BaseUrl { get; set; }

        TimeSpan Timeout { get; set; }

        bool AsyncContinueOnCapturedContext { get; set; }

        Task<IRestResponse<T>> SendAsync<T>(IRestRequest<T> request, CancellationToken cancellationToken) where T : class;
    }
}