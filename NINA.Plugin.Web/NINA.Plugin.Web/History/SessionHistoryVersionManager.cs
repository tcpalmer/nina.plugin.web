using Newtonsoft.Json;
using NINA.Core.Utility;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Web.NINAPlugin.Utility;

namespace Web.NINAPlugin.History {

    public class SessionHistoryVersionManager {
        private static readonly int FLUSH_SLEEP = 3000;
        private static ConcurrentQueue<QueuedItem> versionQueue = new ConcurrentQueue<QueuedItem>();

        private readonly object lockObject = new object();
        private SessionHistory currentSessionHistory;
        private Thread flushThread;
        private bool stopped = false;

        public SessionHistoryVersionManager(SessionHistory initialSessionHistory, string fileName) {
            lock (lockObject) {
                currentSessionHistory = initialSessionHistory;
                QueueVersion(currentSessionHistory, fileName);
            }

            StartFlushThread();
        }

        public void Stop() {
            stopped = true;
            flushQueue();
            flushThread.Abort();
        }

        public SessionHistory GetSessionHistory() {
            return currentSessionHistory;
        }

        public void QueueVersion(SessionHistory sessionHistory, String fileName) {
            try {
                sessionHistory.sessionVersion++;
                SessionHistory clone = cloneSessionHistory(sessionHistory);
                versionQueue.Enqueue(new QueuedItem(sessionHistory, fileName));
                currentSessionHistory = clone;
            }
            catch (Exception e) {
                Logger.Warning($"exception queuing session history version: {e.Message} {e.StackTrace}");
            }
        }

        private SessionHistory cloneSessionHistory(SessionHistory sessionHistory) {
            var serialized = JsonConvert.SerializeObject(sessionHistory);
            return JsonConvert.DeserializeObject<SessionHistory>(serialized);
        }

        private void StartFlushThread() {
            flushThread = new Thread(() => {
                while (!stopped) {
                    flushQueue();
                    Thread.Sleep(FLUSH_SLEEP);
                }

                Logger.Debug("session history version flush thread complete");
            });

            Logger.Debug("session history version flush thread starting");
            flushThread.Start();
        }

        private void flushQueue() {
            if (versionQueue.IsEmpty) {
                return;
            }

            try {
                Logger.Debug($"flushing session history queue, {versionQueue.Count} items");
                QueuedItem next;
                lock (lockObject) {
                    while (versionQueue.TryDequeue(out next)) {
                        int version = next.sessionHistory.sessionVersion;
                        Logger.Debug($"writing session history, version {version}");
                        JsonUtils.WriteJson(next.sessionHistory, next.fileName, true);
                        // TODO: remove this when ready - for now let's see all versions too
                        JsonUtils.WriteJson(next.sessionHistory, $"{next.fileName}_{version}");
                    }
                }
            }
            catch (Exception e) {
                Logger.Warning($"exception flushing session history queue: {e.Message} {e.StackTrace}");
            }
        }
    }

    internal class QueuedItem {
        public SessionHistory sessionHistory { get; }
        public string fileName { get; }

        internal QueuedItem(SessionHistory sessionHistory, string fileName) {
            this.sessionHistory = sessionHistory;
            this.fileName = fileName;
        }
    }
}