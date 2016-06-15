using System.Threading;
using System.Threading.Tasks;
using Slant.Net.Http.Request;
using Slant.Net.Http.Response;

namespace Slant.Net.Http
{
    /// <summary>
    /// Consumer - can send requests and receive responses
    /// </summary>
    public interface IReplyClient
    {
        Task<IRestResponse> SendAsync(IRestRequest request, CancellationToken cancellationToken);
    }
}