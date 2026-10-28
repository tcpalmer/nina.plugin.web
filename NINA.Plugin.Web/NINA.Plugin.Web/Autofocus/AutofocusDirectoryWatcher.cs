using NINA.Core.Utility;
using System;
using System.IO;

namespace Web.NINAPlugin.Autofocus {

    public class AutofocusDirectoryWatcher {
        private string afDirectory;
        private FileSystemWatcher fsWatcher;

        public AutofocusDirectoryWatcher() {
            this.afDirectory = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "AutoFocus");
        }

        public void Start() {
            Stop();
            Watch();
        }

        public void Stop() {
            if (fsWatcher != null) {
                Logger.Debug("web viewer: stopping AF directory watcher");
                fsWatcher.Created -= OnCreated;
                fsWatcher.Renamed -= OnRenamed;
                fsWatcher.EnableRaisingEvents = false;
                fsWatcher.Dispose();
                fsWatcher = null;
            }
        }

        private void Watch() {
            Logger.Info($"web viewer: watching AF directory file: {afDirectory}");

            try {
                fsWatcher = new FileSystemWatcher(afDirectory) {
                    NotifyFilter = NotifyFilters.FileName,
                    Filter = "*.json"
                };

                fsWatcher.Created += OnCreated;
                fsWatcher.Renamed += OnRenamed;
                fsWatcher.EnableRaisingEvents = true;
            } catch (Exception e) {
                Logger.Warning($"failed to watch AF directory for web viewer: {e.Message} {e.StackTrace}");
            }
        }

        public event EventHandler<AutofocusEvent> AutofocusEventSaved;

        private void OnCreated(object sender, FileSystemEventArgs e) {
            Logger.Info("Web session viewer caught autofocus event (file created)");
            onAutofocusEvent(new AutofocusEvent(e.FullPath));
        }

        private void OnRenamed(object sender, RenamedEventArgs e) {
            if (Path.GetExtension(e.FullPath).Equals(".json", StringComparison.OrdinalIgnoreCase)) {
                Logger.Info("Web session viewer caught autofocus event (file renamed)");
                onAutofocusEvent(new AutofocusEvent(e.FullPath));
            }
        }

        public void onAutofocusEvent(AutofocusEvent e) {
            AutofocusEventSaved?.Invoke(this, e);
        }
    }
}