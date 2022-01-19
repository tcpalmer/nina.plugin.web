using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.ComponentModel;
using System.IO;
using Web.NINAPlugin.History;
using Web.NINAPlugin.Http;

namespace Web.NINAPlugin {

    public class ImageSaveWatcher {

        private bool WebPluginEnabled;
        private int PurgeDays;

        private bool initialized = false;
        private SessionList sessionList;
        private SessionHistoryManager sessionHistoryManager;
        private string sessionHome;

        public ImageSaveWatcher(IImageSaveMediator imageSaveMediator) {
            WebPluginEnabled = Properties.Settings.Default.WebPluginEnabled;
            PurgeDays = Properties.Settings.Default.PurgeDays;
            Properties.Settings.Default.PropertyChanged += SettingsChanged;

            sessionHistoryManager = new SessionHistoryManager();
            imageSaveMediator.ImageSaved += ImageSaveMeditator_ImageSaved;
        }

        private void ImageSaveMeditator_ImageSaved(object sender, ImageSavedEventArgs msg) {

            if (!WebPluginEnabled) {
                Logger.Debug("web plugin not enabled");
                return;
            }

            if (msg.MetaData.Image.ImageType != "LIGHT") {
                Logger.Debug("image is not a light, skipping");
                return;
            }

            try {
                if (!initialized) {
                    initialize();
                }

                UpdateSessionHistory(msg);
            }
            catch (Exception ex) {
                Logger.Error($"exception updating session status for web: {ex}");
            }
        }

        // TODO: we need to do a better job of state management here:
        //  - if the web server is stopped, seems like we need to end the current session (deactivating live)
        //    and resetting here.
        //  - if (re)started, we start a new session

        private void initialize() {
            sessionHistoryManager.PurgeHistoryOlderThan(PurgeDays);
            sessionList = sessionHistoryManager.GetSessionList();
            // TODO: deactivate really needs to happen as soon as web server starts
            sessionHistoryManager.DeactivateSessions(sessionList);
            initialized = true;
        }

        private void UpdateSessionHistory(ImageSavedEventArgs msg) {

            SessionHistory sessionHistory;
            Target activeTarget;

            if (sessionHome == null) {
                sessionHistory = new SessionHistory(DateTime.Now);
                Target target = new Target(msg.MetaData.Target.Name);
                sessionHistory.AddTarget(target);
                activeTarget = target;

                // Add latest to the session list
                sessionList.AddSession(sessionHistoryManager.GetSessionDirectoryName(sessionHistory));
                sessionHistoryManager.WriteSessionList(sessionList);
            }
            else {
                sessionHistory = sessionHistoryManager.GetSessionHistory(sessionHome);
                activeTarget = sessionHistory.GetActiveTarget();

                // TODO: a sequence could presumably return to previous target so have to handle
                //    check for existing target by that name and use if found instead of adding new

                if (activeTarget.name != msg.MetaData.Target.Name) {
                    activeTarget = new Target(msg.MetaData.Target.Name);
                    sessionHistory.AddTarget(activeTarget);
                }
            }

            ImageRecord record = new ImageRecord(msg);
            activeTarget.AddImageRecord(record);
            sessionHome = sessionHistoryManager.CreateOrUpdateSessionHistory(sessionHistory);
            sessionHistoryManager.AddThumbnail(sessionHome, record.id, msg.Image);
        }

        void SettingsChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case "WebPluginEnabled":
                    WebPluginEnabled = Properties.Settings.Default.WebPluginEnabled;
                    break;
            }
        }

    }
}
