using System;
using System.Threading;
using System.Threading.Tasks;
using Slant.Net.Http;

namespace Slant.Net.Http
{
    public partial class RestClient
    {
        #region [ Helper ]

		internal static class AsyncHelper
        {
            internal static readonly TaskFactory sTaskFactory = new TaskFactory(CancellationToken.None,
                TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

            public static TResult ExecuteSync<TResult>(Func<Task<TResult>> func)
            {
                return sTaskFactory.StartNew(func).Unwrap().GetAwaiter().GetResult();
            }

            public static void ExecuteSync(Func<Task> func)
            {
                sTaskFactory.StartNew(func).Unwrap().GetAwaiter().GetResult();
            }
        }

        #endregion

        #region [ Synchronous support ]

        public static void ExecuteSync(Func<Task> func, bool continueOnCapturedContext)
        {
            AsyncHelper.ExecuteSync(func);
        }

        public static TResult ExecuteSync<TResult>(Func<Task<TResult>> func)
        {
            return AsyncHelper.ExecuteSync(func);
        }

        public void ExecuteSync(Func<Task> func)
        {
            AsyncHelper.ExecuteSync(func);
        }

        public string GetString(string path)
        {
            return ExecuteSync(() => this.GetStringAsync(path, CancellationToken.None));
        }

        public T Get<T>(string path) where T : class
        {
            return AsyncHelper.ExecuteSync(() => this.GetAsync<T>(path, CancellationToken.None));
        }

        //public string Post(string path, object content)
        //{
        //    return AsyncHelper.ExecuteSync(() => PostAsync(path, EncodeContent(content), CancellationToken.None));
        //}

        //public T Post<T>(string path, object content)
        //    where T : class
        //{
        //    return AsyncHelper.ExecuteSync(() => PostAsync<T>(path, EncodeContent(content), CancellationToken.None));
        //}

        #endregion
    }
}