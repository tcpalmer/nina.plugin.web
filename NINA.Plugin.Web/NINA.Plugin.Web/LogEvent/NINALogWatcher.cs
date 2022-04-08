using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Web.NINAPlugin.LogEvent {

    public class NINALogWatcher {
        private NINALogMessageProcessor processor;
        private string logDirectory;
        private string activeLogFile;
        private Thread watcherThread = null;
        private bool stopped = false;
        private AutoResetEvent autoReset = null;

        public NINALogWatcher(NINALogMessageProcessor processor) {
            this.processor = processor;
            this.logDirectory = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs");
            this.activeLogFile = getActiveLogFile(logDirectory);
        }

        public void Start() {
            Stop();

            if (activeLogFile != null) {
                Logger.Info($"web viewer: watching log file: {activeLogFile}");
                Watch(logDirectory, activeLogFile);
            }
        }

        public void Stop() {
            if (watcherThread != null) {
                Logger.Debug("web viewer: stopping log watcher");
                stopped = true;
            }
        }

        private void Watch(string logDirectory, string activeLogFile) {
            watcherThread = new Thread(() => {
                try {
                    autoReset = new AutoResetEvent(false);
                    FileSystemWatcher fsWatcher = new FileSystemWatcher(logDirectory);
                    fsWatcher.Filter = activeLogFile;
                    fsWatcher.EnableRaisingEvents = true;
                    fsWatcher.Changed += (s, e) => autoReset.Set();

                    FileStream fs = new FileStream(activeLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using (StreamReader sr = new StreamReader(fs)) {
                        List<string> lines = new List<string>();
                        while (!stopped) {
                            string line = sr.ReadLine();

                            if (line != null) {
                                lines.Add(line);
                            }
                            else {
                                if (lines.Count > 0) {
                                    processor.processLogMessages(lines);
                                    lines.Clear();
                                }

                                autoReset.WaitOne(2000);
                            }
                        }

                        autoReset.Close();
                    }
                }
                catch (Exception e) {
                    Logger.Warning($"failed to process log messages for web viewer: {e.Message} {e.StackTrace}");
                }
            });

            watcherThread.Start();
        }

        private string getActiveLogFile(string logDirectory) {
            // NINA log files match yyyyMMdd-HHmmss-VERSION.PID.log - see NINA.Core.Utility.Logger

            try {
                string version = CoreUtil.Version;
                int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                Regex re = new Regex(@"^\d{8}-\d{6}-" + $"{version}.{processId}.log$", RegexOptions.Compiled);

                List<string> fileList = new List<string>(Directory.GetFiles(logDirectory));
                foreach (string file in fileList) {
                    if (re.IsMatch(Path.GetFileName(file))) {
                        return file;
                    }
                }

                Logger.Warning($"failed to find active NINA log file in {logDirectory}, cannot process log events for web viewer");
                return null;
            }
            catch (Exception e) {
                Logger.Warning($"failed to find active NINA log file in {logDirectory}, cannot process log events for web viewer: {e.Message} {e.StackTrace}");
                return null;
            }
        }
    }
}