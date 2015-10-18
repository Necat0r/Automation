using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Support
{
    class AsyncLock
    {
        public struct Releaser : IDisposable
        {
            private readonly AsyncLock mLock;

            internal Releaser(AsyncLock asyncLock) { mLock = asyncLock; }

            public void Dispose()
            {
                if (mLock != null)
                    mLock.Release();
            }
        }

        private Object mLock = new Object();
        volatile bool mLocked = false;

        private readonly Task<Releaser> mReleaser;

        public AsyncLock()
        {
            mReleaser = Task.FromResult(new Releaser(this));
        }

        public Task<Releaser> AquireAsync(int timeout, string name)
        {
            var wait = Task.Run(async () =>
            {
                while (true)
                {
                    lock (mLock)
                    {
                        if (mLocked == false)
                        {
                            mLocked = true;
                            break;
                        }
                    }

                    // FIXME - Can be done without polling.
                    await Task.Delay(100);
                }
            });

            return wait.IsCompleted ?
                mReleaser :
                wait.ContinueWith(
                    (_, state) => new Releaser((AsyncLock)state),
                    this,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        private void Release()
        {
            lock (mLock)
            {
                mLocked = false;
            }
        }
    }
}
