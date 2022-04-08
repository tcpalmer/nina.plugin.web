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
            //processor.NINALogEventSaved += handleEvent;
            autofocusDirectoryWatcher.Start();
        }

        public void Stop() {
            autofocusDirectoryWatcher.Stop();
            //processor.NINALogEventSaved -= handleEvent;
        }

    }
}