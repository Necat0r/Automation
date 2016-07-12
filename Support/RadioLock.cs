using Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Support
{
    public class RadioLock : IDisposable
    {
        public struct Releaser : IDisposable
        {
            private readonly RadioLock mLock;

            internal Releaser(RadioLock radioLock) { mLock = radioLock; }

            public void Dispose()
            {
                if (mLock != null)
                    mLock.Release();
            }
        }

        private static RadioLock mInstance;

        private Object mLock = new Object();
        volatile bool mLocked = false;
        string mOwner;
        int mTimeout;
        Thread mTimeoutThread;
        AutoResetEvent mReleaseEvent = new AutoResetEvent(false);

        private readonly Task<Releaser> mReleaser;

        public RadioLock()
        {
            mInstance = this;

            mReleaser = Task.FromResult(new Releaser(this));
        }

        public void Dispose()
        {
            mReleaseEvent.Dispose();
        }

        public static RadioLock Instance
        {
            get {
                if (mInstance == null)
                    mInstance = new RadioLock();
                return mInstance;
            }
        }

        public Task<Releaser> AquireAsync(int timeout, string name)
        {
            var wait = Task.Run(async () =>
            {
                while (!TryEnter(timeout, name))
                {
                    // Don't busy wait
                    // FIXME - Implement a proper awaitable lock queue.
                    await Task.Delay(100);
                }
            });

            return wait.IsCompleted ?
                mReleaser :
                wait.ContinueWith(
                    (_, state) => new Releaser((RadioLock)state),
                    this,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        public bool TryEnter(int timeout, string name)
        {
            // Non-thread safe check
            if (mLocked)
                return false;

            lock (mLock)
            {
                if (mLocked)
                {
                    // Someone beat us to it!
                    return false;
                }

                mLocked = true;
            }

            // Capture current thread
            mOwner = name;

            mTimeout = timeout;
            Log.Debug("RadioLock - Acquired by thread {0} for {1} milliseconds", mOwner, mTimeout);

            mTimeoutThread = new Thread(Timeout);
            mReleaseEvent.Reset();
            mTimeoutThread.Start();

            return true;
        }

        public void Release()
        {
            lock (mLock)
            {
                mLocked = false;
            }
            mOwner = null;

            mReleaseEvent.Set();

            Log.Debug("RadioLock - Released");
        }

        private void Timeout()
        {
            bool signal = mReleaseEvent.WaitOne(mTimeout);
            if (!signal)
            {
                Log.Error("Lock wasn't released in time. Thread: {0}", mOwner);
                Release();
                mReleaseEvent.Reset();
            }
        }
    }
}
