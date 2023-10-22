using NINA.Core.Utility;
using System.IO;
using System.Text.RegularExpressions;
using Web.NINAPlugin.History;
using Web.NINAPlugin.LogEvent;
using Web.NINAPlugin.Utility;

namespace Web.NINAPlugin {

    public class NINAEventWatcher {

        private object lockobj = new object();

        private SessionHistoryManager sessionHistoryManager;
        private NINALogMessageProcessor processor;
        private NINALogWatcher logWatcher;
        private string sessionHome;
        private FileSystemWatcher rolloverWatcher;

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
            WatchForRollover(LogUtils.GetLogDirectory());
        }

        public void Stop(bool addStopEvent) {
            if (addStopEvent) {
                handleEvent(null, new NINALogEvent(NINALogEvent.NINA_STOP));
            }

            logWatcher.Stop();
            processor.NINALogEventSaved -= handleEvent;

            if (rolloverWatcher != null) {
                rolloverWatcher.EnableRaisingEvents = false;
                rolloverWatcher.Dispose();
                rolloverWatcher = null;
            }
        }

        private void handleEvent(object sender, NINALogEvent e) {
            if (sessionHome == null) {
                Logger.Warning("web session event watcher not initialized");
                return;
            }

            sessionHistoryManager.UpdateAddEvent(sessionHome, e);
        }

        public void WatchForRollover(string logDirectory) {
            Logger.Debug($"watching for log rollovers in {logDirectory}");
            rolloverWatcher = new FileSystemWatcher();
            rolloverWatcher.Path = logDirectory;
            rolloverWatcher.NotifyFilter = NotifyFilters.Attributes
                                         | NotifyFilters.CreationTime
                                         | NotifyFilters.DirectoryName
                                         | NotifyFilters.FileName
                                         | NotifyFilters.LastAccess
                                         | NotifyFilters.LastWrite
                                         | NotifyFilters.Security
                                         | NotifyFilters.Size;

            rolloverWatcher.Filter = "*.log";
            rolloverWatcher.Created += new FileSystemEventHandler(LogFileRolled);
            rolloverWatcher.EnableRaisingEvents = true;
        }

        public void LogFileRolled(object source, FileSystemEventArgs e) {
            string filename = Path.GetFileName(e.FullPath);
            Regex re = new Regex(LogUtils.GetLogFileRE(), RegexOptions.None);

            if (re.IsMatch(filename)) {
                lock (lockobj) {
                    Logger.Info($"web viewer detected log roll, new file is {filename}");
                    Stop(false);
                    logWatcher = new NINALogWatcher(processor);
                    Start();
                }
            }
            else {
                Logger.Warning($"web viewer detected new file in log directory but doesn't match pattern: {filename}, ignored");
            }
        }

    }
}