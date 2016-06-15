using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Slant.Net.Http.Request;
using Slant.Net.Http.Response;

namespace Slant.Net.Http
{
    /// <summary>
    /// Basic RestClient extensions
    /// </summary>
    public static class RestClientExtensions
    {
        public static async Task<string> GetStringAsync(this IRestClient client, string path, CancellationToken cancellationToken)
        {
            var request = new RestRequest(HttpMethod.Get)
            {
                Path = path
            };
            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(client.AsyncContinueOnCapturedContext);
            return response.Body;
        }

        public static Task<string> GetStringAsync(this IRestClient client, string path)
        {
            return GetStringAsync(client, path, CancellationToken.None);
        }

        public static async Task<T> ExecuteAsync<T>(this IRestClient client, IRestRequest<T> request, CancellationToken cancellationToken)
            where T : class
        {
            var response = await client.SendAsync<T>(request, cancellationToken).ConfigureAwait(client.AsyncContinueOnCapturedContext);
            return response.Data;
        }

        public static async Task<T> GetAsync<T>(this IRestClient client, string path, CancellationToken cancellationToken) where T : class
        {
            var request = new RestRequest<T>(HttpMethod.Get)
            {
                Path = path
            };
            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(client.AsyncContinueOnCapturedContext);
            return response.Data;
        }

        public static Task<T> GetAsync<T>(this IRestClient client, string path) where T : class
        {
            return client.GetAsync<T>(path, CancellationToken.None);
        }

        public static async Task<IRestResponse> PostAsync(this IRestClient client, string path, HttpContent content,
            CancellationToken cancellationToken)
        {
            var request = new RestPostRequest(path, content);
            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(client.AsyncContinueOnCapturedContext);
            return response;
        }

        public static async Task<IRestResponse<T>> PostAsync<T>(this IRestClient client, string path, HttpContent content,
            CancellationToken cancellationToken) where T : class 
        {
            var request = new RestPostRequest<T>(path, content);
            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(client.AsyncContinueOnCapturedContext);
            return response;
        }
    }
}