using Web.NINAPlugin.History;

namespace Web.NINAPlugin.Autofocus {

    public class AutofocusEventWatcher {
        private SessionHistoryManager sessionHistoryManager;
        private AutofocusDirectoryWatcher autofocusDirectoryWatcher;
        private string sessionHome;

        public AutofocusEventWatcher() {
            sessionHistoryManager = new SessionHistoryManager();
            autofocusDirectoryWatcher = new AutofocusDirectoryWatcher();
        }

        public void setSessionHome(string sessionHome) {
            this.sessionHome = sessionHome;
        }

        public void Start() {
            autofocusDirectoryWatcher.AutofocusEventSaved += handleEvent;
            autofocusDirectoryWatcher.Start();
        }

        public void Stop() {
            autofocusDirectoryWatcher.Stop();
            autofocusDirectoryWatcher.AutofocusEventSaved -= handleEvent;
        }

        private void handleEvent(object sender, AutofocusEvent e) {
            sessionHistoryManager.UpdateAddAutofocusEvent(sessionHome, e);
        }
    }
}