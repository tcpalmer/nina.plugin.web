using NINA.Core.Utility;
using NINA.Equipment.Model;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.ComponentModel;
using Web.NINAPlugin.History;

namespace Web.NINAPlugin {

    public class ImageSaveWatcher {
        private bool WebPluginEnabled;
        private bool NonLightsEnabled;

        private SessionHistoryManager sessionHistoryManager;
        private string sessionHome;

        public ImageSaveWatcher(IImageSaveMediator imageSaveMediator) {
            WebPluginEnabled = Properties.Settings.Default.WebPluginEnabled;
            NonLightsEnabled = Properties.Settings.Default.NonLights;
            Properties.Settings.Default.PropertyChanged += SettingsChanged;

            sessionHistoryManager = new SessionHistoryManager();
            imageSaveMediator.ImageSaved += ImageSaveMeditator_ImageSaved;
        }

        public void setSessionHome(string sessionHome) {
            this.sessionHome = sessionHome;
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
                UpdateSessionHistory(msg);
            }
            catch (Exception ex) {
                Logger.Error($"exception updating session status for web viewer: {ex.Message} {ex.StackTrace}");
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

        private void UpdateSessionHistory(ImageSavedEventArgs msg) {
            string targetName = getTargetName(msg);
            ImageRecord record = new ImageRecord(msg);
            sessionHistoryManager.UpdateAddImageRecord(sessionHome, targetName, record);
            sessionHistoryManager.AddThumbnail(sessionHome, record.id, msg.Image);
        }

        private string getTargetName(ImageSavedEventArgs msg) {
            if (string.IsNullOrEmpty(msg.MetaData.Target?.Name)) {
                return string.IsNullOrEmpty(msg.Filter)
                    ? msg.MetaData.Image.ImageType
                    : $"{msg.MetaData.Image.ImageType}-{msg.Filter}";
            }

            return msg.MetaData.Target.Name;
        }

        private void SettingsChanged(object sender, PropertyChangedEventArgs e) {
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