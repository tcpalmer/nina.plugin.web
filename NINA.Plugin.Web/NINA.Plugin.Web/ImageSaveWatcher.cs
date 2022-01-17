using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using Web.NINAPlugin.History;

namespace Web.NINAPlugin {

    public class ImageSaveWatcher {

        private SessionHistoryManager sessionHistoryManager;
        private string sessionHome;

        public ImageSaveWatcher(IImageSaveMediator imageSaveMediator) {
            sessionHistoryManager = new SessionHistoryManager();
            imageSaveMediator.ImageSaved += ImageSaveMeditator_ImageSaved;
        }

        private void ImageSaveMeditator_ImageSaved(object sender, ImageSavedEventArgs msg) {

            // TODO: bail if not enabled

            // TODO: need to create/update a separate JSON file with list of all sessions (updated in purge too)

            // TODO: need a way to mark a target as 'live' or not
            //    have to save the current target and compare
            //    just repurpose activeTargetId: if present, that target is live/active
            //    when NINA shuts down, set activeTargetId to null on final write of active session

            // TODO: a sequence could presumably return to previous target so have to handle
            //    check for existing target by that name and use if found instead of adding new

            if (msg.MetaData.Image.ImageType != "LIGHT") {
                Logger.Debug("image is not a light, skipping");
                return;
            }

            try {
                UpdateSessionHistory(msg);
            }
            catch (Exception ex) {
                Logger.Error($"exception updating session status for web: {ex}");
            }
        }

        private void UpdateSessionHistory(ImageSavedEventArgs msg) {

            SessionHistory sessionHistory;
            Target activeTarget;

            if (sessionHome == null) {
                sessionHistory = new SessionHistory(DateTime.Now);
                Target target = new Target(msg.MetaData.Target.Name);
                sessionHistory.AddTarget(target);
                activeTarget = target;
            }
            else {
                sessionHistory = sessionHistoryManager.GetSessionHistory(sessionHome);
                activeTarget = sessionHistory.GetActiveTarget();
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
    }
}
