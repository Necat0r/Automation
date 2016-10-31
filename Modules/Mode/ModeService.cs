using Module;
using System;

namespace Mode
{
    // TODO - Add pending mode support. For instance activating away mode a few minutes after leaving.
    // Would this potentially be handled via a separate mode instead? Pending_Away so validation etc can be applied beforehand
    // to check that windows are closed etc.

    public class ModeService : ServiceBase
    {
        public enum Mode
        {
            Off,
            Away,
            Normal,
            Night,
            Cinema,
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

        private Mode mMode;

        public ModeService(ServiceCreationInfo info)
            : base("mode", info)
        {

        }

        public Mode CurrentMode
        {
            get { return mMode; }
            set { ChangeMode(value); }
        }

        private void ChangeMode(Mode mode)
        {
            if (mMode != mode)
            {
                Mode oldMode = mMode;
                mMode = mode;

                if (OnModeChange != null)
                    OnModeChange(this, new ModeEvent(mMode, oldMode));
            }
        }
    }
}
