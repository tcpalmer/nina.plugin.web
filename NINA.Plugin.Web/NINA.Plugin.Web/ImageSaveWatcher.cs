using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.ComponentModel;
using Web.NINAPlugin.History;

namespace Web.NINAPlugin {

    public class ImageSaveWatcher {

        private bool WebPluginEnabled;

        private bool initialized = false;
        private SessionList sessionList;
        private SessionHistoryManager sessionHistoryManager;
        private string sessionHome;

        public ImageSaveWatcher(IImageSaveMediator imageSaveMediator) {
            WebPluginEnabled = Properties.Settings.Default.WebPluginEnabled;
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

        private void initialize() {
            sessionList = sessionHistoryManager.GetSessionList();
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
