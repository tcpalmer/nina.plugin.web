using NINA.Core.Utility;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.ComponentModel;
using Web.NINAPlugin.History;

namespace Web.NINAPlugin {

    public class ImageSaveWatcher {

        private bool WebPluginEnabled;
        private bool NonLightsEnabled;

        private bool initialized = false;
        IProfileService profileService;
        private SessionList sessionList;
        private SessionHistoryManager sessionHistoryManager;
        private string sessionHome;

        public ImageSaveWatcher(IProfileService profileService, IImageSaveMediator imageSaveMediator) {
            WebPluginEnabled = Properties.Settings.Default.WebPluginEnabled;
            NonLightsEnabled = Properties.Settings.Default.NonLights;
            Properties.Settings.Default.PropertyChanged += SettingsChanged;

            this.profileService = profileService;
            sessionHistoryManager = new SessionHistoryManager();
            imageSaveMediator.ImageSaved += ImageSaveMeditator_ImageSaved;
        }

        private void ImageSaveMeditator_ImageSaved(object sender, ImageSavedEventArgs msg) {

            if (!WebPluginEnabled) {
                Logger.Debug("web plugin not enabled");
                return;
            }

            if (!isEnabledImageType(msg.MetaData.Image.ImageType)) {
                Logger.Debug($"image type not enabled, skipping: {msg.MetaData.Image.ImageType}");
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

        private bool isEnabledImageType(string imageType) {

            if (string.IsNullOrEmpty(imageType)) {
                return false;
            }

            if (!NonLightsEnabled) {
                return imageType == CaptureSequence.ImageTypes.LIGHT;
            }

            switch (imageType) {
                case CaptureSequence.ImageTypes.LIGHT:
                case CaptureSequence.ImageTypes.FLAT:
                case CaptureSequence.ImageTypes.DARK:
                case CaptureSequence.ImageTypes.DARKFLAT:
                    return true;
                case CaptureSequence.ImageTypes.BIAS:
                case CaptureSequence.ImageTypes.SNAPSHOT:
                    return false;
            }

            return false;
        }

        private void initialize() {
            sessionList = sessionHistoryManager.GetSessionList();
            initialized = true;
        }

        private void UpdateSessionHistory(ImageSavedEventArgs msg) {

            SessionHistory sessionHistory;
            Target activeTarget;

            if (sessionHome == null) {
                sessionHistory = new SessionHistory(DateTime.Now, profileService);
                Target target = new Target(getTargetName(msg));
                sessionHistory.AddTarget(target);
                activeTarget = target;

                // Add latest to the session list
                sessionList.AddSession(sessionHistoryManager.GetSessionDirectoryName(sessionHistory));
                sessionHistoryManager.WriteSessionList(sessionList);
            }
            else {
                sessionHistory = sessionHistoryManager.GetSessionHistory(sessionHome);
                activeTarget = sessionHistory.GetActiveTarget();

                string targetName = getTargetName(msg);
                if (activeTarget.name != targetName) {
                    activeTarget = new Target(targetName);
                    sessionHistory.AddTarget(activeTarget);
                }
            }

            ImageRecord record = new ImageRecord(msg);
            activeTarget.AddImageRecord(record);
            sessionHome = sessionHistoryManager.CreateOrUpdateSessionHistory(sessionHistory);
            sessionHistoryManager.AddThumbnail(sessionHome, record.id, msg.Image);
        }

        private string getTargetName(ImageSavedEventArgs msg) {
            if (string.IsNullOrEmpty(msg.MetaData.Target.Name)) {
                return string.IsNullOrEmpty(msg.Filter)
                    ? msg.MetaData.Image.ImageType
                    : $"{msg.MetaData.Image.ImageType}-{msg.Filter}";
            }

            return msg.MetaData.Target.Name;
        }

        void SettingsChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case "WebPluginEnabled":
                    WebPluginEnabled = Properties.Settings.Default.WebPluginEnabled;
                    break;
                case "NonLights":
                    NonLightsEnabled = Properties.Settings.Default.NonLights;
                    break;
            }
        }

    }
}
