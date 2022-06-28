using NINA.Core.Utility;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using Web.NINAPlugin.History;

namespace Web.NINAPlugin {

    public class ImageSaveWatcher {
        private IImageSaveMediator imageSaveMediator;
        private IPluginOptionsAccessor pluginSettings;
        private bool running = false;

        private SessionHistoryManager sessionHistoryManager;
        private string sessionHome;

        public ImageSaveWatcher(IImageSaveMediator imageSaveMediator, IPluginOptionsAccessor pluginSettings) {
            this.imageSaveMediator = imageSaveMediator;
            this.pluginSettings = pluginSettings;

            sessionHistoryManager = new SessionHistoryManager();
        }

        public void setSessionHome(string sessionHome) {
            this.sessionHome = sessionHome;
        }

        public void Start() {
            if (sessionHome == null) {
                Logger.Warning("can't start image save watcher, sessionHome not set");
            }

            Stop();
            Logger.Debug("starting web session image save watcher");
            imageSaveMediator.ImageSaved += ImageSaveMeditator_ImageSaved;
            running = true;
        }

        public void Stop() {
            if (running) {
                Logger.Debug("stopping web session image save watcher");
                imageSaveMediator.ImageSaved -= ImageSaveMeditator_ImageSaved;
                running = false;
            }
        }

        private void ImageSaveMeditator_ImageSaved(object sender, ImageSavedEventArgs msg) {
            if (!isPluginActive()) {
                Logger.Debug("web plugin not active");
                return;
            }

            if (!isEnabledImageType(msg.MetaData.Image.ImageType)) {
                Logger.Debug($"image type not enabled, skipping: {msg.MetaData.Image.ImageType}");
                return;
            }

            if (sessionHome == null) {
                Logger.Warning("can't add image to web session history, no active session");
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

            if (!isNonLightsEnabled()) {
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

        private bool isPluginActive() {
            string state = pluginSettings.GetValueString(nameof(WebPlugin.WebPluginState), WebPlugin.WebPluginStateDefault);
            return state.Equals(WebPlugin.WebPluginStateON) || state.Equals(WebPlugin.WebPluginStateSHARE);
        }

        private bool isNonLightsEnabled() {
            return pluginSettings.GetValueBoolean(nameof(WebPlugin.NonLights), WebPlugin.NonLightsDefault);
        }
    }
}