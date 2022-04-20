using NINA.Core.Utility;
using Web.NINAPlugin.History;
using Web.NINAPlugin.LogEvent;

namespace Web.NINAPlugin {

    public class NINAEventWatcher {
        private SessionHistoryManager sessionHistoryManager;
        private NINALogMessageProcessor processor;
        private NINALogWatcher logWatcher;
        private string sessionHome;

        public NINAEventWatcher() {
            this.sessionHistoryManager = new SessionHistoryManager();
            this.processor = new NINALogMessageProcessor();
            this.logWatcher = new NINALogWatcher(processor);
        }

        public void setSessionHome(string sessionHome) {
            this.sessionHome = sessionHome;
            handleEvent(null, new NINALogEvent(NINALogEvent.NINA_START));
        }

        public void Start() {
            processor.NINALogEventSaved += handleEvent;
            logWatcher.Start();
        }

        public void Stop(bool addStopEvent) {
            if (addStopEvent) {
                handleEvent(null, new NINALogEvent(NINALogEvent.NINA_STOP));
            }

            logWatcher.Stop();
            processor.NINALogEventSaved -= handleEvent;
        }

        private void handleEvent(object sender, NINALogEvent e) {
            if (sessionHome == null) {
                Logger.Warning("web session event watcher not initialized");
                return;
            }

            sessionHistoryManager.UpdateAddEvent(sessionHome, e);
        }
    }
}