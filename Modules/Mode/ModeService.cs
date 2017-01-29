using Module;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Support;
using System.Linq;
using Logging;

namespace Mode
{
    public class ModeService : ServiceBase
    {
        public class Mode
        {
            public Mode(string name)
            {
                Name = name;
            }

            public string Name { get; set; }
        }

        public class ModeEvent : EventArgs
        {
            public ModeService.Mode NewMode;
            public ModeService.Mode OldMode;

            public ModeEvent(ModeService.Mode newMode, ModeService.Mode oldMode)
            {
                NewMode = newMode;
                OldMode = oldMode;
            }
        }
        public event EventHandler<ModeEvent> OnModeChange;

        struct ModeChange
        {
            public ModeChange(Mode mode, DateTime activationTime)
            {
                this.Mode = mode;
                this.ActivationTime = activationTime;
            }

            public Mode Mode;
            public DateTime ActivationTime;

        }
        private List<ModeChange> mChangeQueue;
        private AsyncEvent mQueueEvent;

        private Mode mCurrentMode;

        public ModeService(ServiceCreationInfo info)
        : base("mode", info)
        {
            mCurrentMode = new Mode("<none>");
            mChangeQueue = new List<ModeChange>();
            mQueueEvent = new AsyncEvent();

            Task.Run(async () =>
            {
                while (true)
                {
                    // Retry once we have something in the queue.
                    if (mChangeQueue.Count == 0)
                    {
                        await mQueueEvent.Wait();
                        continue;
                    }

                    ModeChange change;
                    var delay = TimeSpan.Zero;

                    lock (mChangeQueue)
                    {
                        change = mChangeQueue[0];
                        delay = change.ActivationTime - DateTime.Now;
                        mQueueEvent.Reset();

                        if (delay <= TimeSpan.Zero)
                            mChangeQueue.RemoveAt(0);
                    }

                    if (delay > TimeSpan.Zero)
                    {
                        using (var delayTokenSource = new CancellationTokenSource())
                        {
                            var tasks = new Task[] { mQueueEvent.Wait(), Task.Delay(delay, delayTokenSource.Token) };
                            await Task.WhenAny(tasks);
                        }
                        continue;
                    }

                    CurrentMode = change.Mode;
                }
            });
        }

        public void CancelPendingChanges()
        {
            lock (mChangeQueue)
            {
                mChangeQueue.Clear();
                mQueueEvent.Reset();
            }
        }

        public Mode CurrentMode
        {
            get { return mCurrentMode; }
            set { ChangeMode(value); }
        }

        public void QueueChange(Mode mode, int seconds)
        {
            Log.Debug("Queuing mode: {0}, timeout: {1}", mode.Name, seconds);

            var activationTime = DateTime.Now + new TimeSpan(0, 0, seconds);

            lock (mChangeQueue)
            {
                mChangeQueue.Add(new ModeChange(mode, activationTime));
                mChangeQueue.OrderBy(x => x.ActivationTime);
                mQueueEvent.Set();
            }
        }

        private void ChangeMode(Mode mode)
        {
            if (mCurrentMode == mode)
            {
                Log.Debug("Already in mode: {0}", mode.Name);
                return;
            }

            Log.Debug("Changing mode, old: {0}, new: {1}", mCurrentMode.Name, mode.Name);

            Mode oldMode = mCurrentMode;
            mCurrentMode = mode;

            if (OnModeChange != null)
                OnModeChange(this, new ModeEvent(mCurrentMode, oldMode));
        }
    }
}
