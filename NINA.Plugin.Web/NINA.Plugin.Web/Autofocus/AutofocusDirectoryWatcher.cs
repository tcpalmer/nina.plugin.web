using NINA.Core.Utility;
using System;
using System.IO;
using System.Threading;

namespace Web.NINAPlugin.Autofocus {

    public class AutofocusDirectoryWatcher {
        private string afDirectory;
        private Thread watcherThread = null;

        public AutofocusDirectoryWatcher() {
            this.afDirectory = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "AutoFocus");
        }

        public void Start() {
            Stop();

            Logger.Info($"web viewer: watching AF directory file: {afDirectory}");
            Watch();
        }

        public void Stop() {
            if (watcherThread != null) {
                Logger.Debug("web viewer: stopping AF directory watcher");
                watcherThread.Abort();
            }
        }

        // AF files: 2022-02-10--00-04-42.json
        /*
            reportFileWatcher = new FileSystemWatcher() {
                Path = WPF.Base.ViewModel.AutoFocus.AutoFocusVM.ReportDirectory,
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.json",
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            reportFileWatcher.Created += ReportFileWatcher_Created;
            reportFileWatcher.Deleted += ReportFileWatcher_Deleted;

        private void ReportFileWatcher_Created(object sender, FileSystemEventArgs e) {
            Logger.Debug($"New AutoFocus chart created at {e.FullPath}");
            var item = new WPF.Base.ViewModel.Imaging.AutoFocusToolVM.Chart(Path.GetFileName(e.FullPath), e.FullPath);

            lock (lockobj) {
                ChartList.Insert(0, item);
                SelectedChart = item;
            }

            _ = LoadChart();
        }
         */

        private void Watch() {
            watcherThread = new Thread(() => {
                try { // TODO: is this only going to run once?  Or does it even need a Thread?
                    FileSystemWatcher fsWatcher = new FileSystemWatcher(afDirectory);
                    fsWatcher.NotifyFilter = NotifyFilters.LastWrite;
                    fsWatcher.Filter = "*.json";
                    fsWatcher.Changed += new FileSystemEventHandler(OnChanged);
                    fsWatcher.EnableRaisingEvents = true;
                    fsWatcher.Dispose();
                }
                catch (Exception e) {
                    Logger.Warning($"failed to watch AF directory for web viewer: {e.Message} {e.StackTrace}");
                }
            });

            watcherThread.Start();
        }

        private void OnChanged(object sender, FileSystemEventArgs e) {
            // TODO: this needs to call callback to AutofocusEventWatcher method
            Logger.Debug($"GOT NEW AF FILE: {e.Name} {e.ChangeType}");
        }
    }
}