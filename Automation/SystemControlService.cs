using Logging;
using Module;
using System;
using System.ComponentModel;
using System.Deployment.Application;

namespace Automation
{
    class SystemControlService : ServiceBase
    {
        public class Status
        {
            public string CurrentVersion { get; set; }
            public string AvailableVersion { get; set; }
            public bool UpdateAvailable { get; set; }
        }

        private Status mStatus;

        private bool mWorking;

        public SystemControlService(ServiceCreationInfo info)
        : base("system", info)
        {
            mWorking = false;

            RefreshStatus();

            Log.Info("Application is running version: " + mStatus.CurrentVersion);
            if (mStatus.UpdateAvailable)
                Log.Info("There is an update available");
        }

        private void RefreshStatus()
        {
            mStatus = new Status();

            bool networkDeployed = ApplicationDeployment.IsNetworkDeployed;
            if (networkDeployed)
            {
                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

                mStatus.CurrentVersion = ad.CurrentVersion.ToString();
                mStatus.AvailableVersion = mStatus.CurrentVersion;

                try
                {
                    UpdateCheckInfo updateInfo = ad.CheckForDetailedUpdate();
                    mStatus.AvailableVersion = updateInfo.AvailableVersion.ToString();

                    mStatus.UpdateAvailable = updateInfo.UpdateAvailable;
                }
                catch (InvalidOperationException)
                { }
            }
        }

        [ServiceGetContractAttribute("status")]
        public Status OnGetStatus()
        {
            RefreshStatus();

            return mStatus;
        }

        [ServicePutContract("update")]
        public bool OnUpdateRequest()
        {
            Log.Debug("ControlService: " + this);
            RefreshStatus();

            if (mWorking)
            {
                Log.Info("Discarding surplus udpate request");
                return false;
            }

            if (!mStatus.UpdateAvailable)
            {
                Log.Info("Attempting to update but there are no new versions available");
                return false;
            }

            bool networkDeployed = ApplicationDeployment.IsNetworkDeployed;
            if (!networkDeployed)
            {
                Log.Info("The application is not network deployed");
                return false;
            }

            mWorking = true;

            Log.Info("Update requested");

            var deployment = ApplicationDeployment.CurrentDeployment;

            deployment.UpdateCompleted += new AsyncCompletedEventHandler((source, args) => {
                Log.Info("==============================================================");
                Log.Info("=========================RESTARTING===========================");
                Log.Info("==============================================================");
                System.Windows.Forms.Application.Restart();
            });

            Log.Info("==============================================================");
            Log.Info("==========================UPDATING============================");
            Log.Info("==============================================================");
            deployment.UpdateAsync();

            return true;
        }

        [ServicePutContract("restart")]
        public void OnRestartRequest()
        {
            if (mWorking)
            {
                Log.Info("Discarding surplus restart request");
                return;
            }

            mWorking = true;

            Log.Info("Restart requested");

            Log.Info("==============================================================");
            Log.Info("=========================RESTARTING===========================");
            Log.Info("==============================================================");
            System.Windows.Forms.Application.Restart();
        }
    }
}
