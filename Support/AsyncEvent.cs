using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Support
{
    public class AsyncEvent
    {
        private HashSet<CancellationTokenSource> mTokenSources;
        private volatile bool mSet = false;

        public AsyncEvent()
        {
            mTokenSources = new HashSet<CancellationTokenSource>();
        }

        public Task Wait()
        {
            CancellationToken dummy;
            return Wait(dummy);
        }

        public Task Wait(CancellationToken externalToken)
        {
            var task = Task.Run(async () =>
            {
                // Early out if it's already set
                if (mSet)
                    return;

                using (var setTokenSource = new CancellationTokenSource())
                {
                    lock (mTokenSources)
                    {
                        mTokenSources.Add(setTokenSource);
                    }

                    var setToken = setTokenSource.Token;
                    while (!externalToken.IsCancellationRequested && !setTokenSource.IsCancellationRequested)
                    {
                        // FIXME - Should be a better way of creating an infinite task with a cancellation token.
                        var externalDelayTask = Task.Delay(100000, externalToken);
                        var setDelayTask = Task.Delay(100000, setToken);

                        await Task.WhenAny(new Task[] { externalDelayTask, setDelayTask });
                    }

                    lock (mTokenSources)
                    {
                        mTokenSources.Remove(setTokenSource);
                    }
                }
            });

            return task;
        }

        public void Set()
        {
            mSet = true;

            lock (mTokenSources)
            {
                var copy = new List<CancellationTokenSource>(mTokenSources);

                foreach (var source in copy)
                    source.Cancel();
            }
        }

        public void Reset()
        {
            mSet = false;
        }
    }
}
