using NINA.Core.Utility;
using Web.NINAPlugin.History;

namespace Web.NINAPlugin.Autofocus {

    public class AutofocusEventWatcher {
        private SessionHistoryManager sessionHistoryManager;
        private AutofocusDirectoryWatcher autofocusDirectoryWatcher;
        private string sessionHome;
        private bool running = false;

        public AutofocusEventWatcher() {
            sessionHistoryManager = new SessionHistoryManager();
            autofocusDirectoryWatcher = new AutofocusDirectoryWatcher();
        }

        public void setSessionHome(string sessionHome) {
            this.sessionHome = sessionHome;
        }

        public void Start() {
            Stop();
            autofocusDirectoryWatcher.AutofocusEventSaved += handleEvent;
            autofocusDirectoryWatcher.Start();
            running = true;
        }

        public void Stop() {
            if (running) {
                autofocusDirectoryWatcher.Stop();
                autofocusDirectoryWatcher.AutofocusEventSaved -= handleEvent;
                running = false;
            }
        }

        private void handleEvent(object sender, AutofocusEvent e) {
            if (sessionHome == null) {
                Logger.Warning("web session autofocus event watcher not initialized");
                return;
            }

            sessionHistoryManager.UpdateAddAutofocusEvent(sessionHome, e);
        }
    }
}