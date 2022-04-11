using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Web.NINAPlugin.Autofocus;
using Web.NINAPlugin.Http;
using Web.NINAPlugin.LogEvent;
using Web.NINAPlugin.Utility;

namespace Web.NINAPlugin.History {

    public class SessionHistoryManager {
        private static SessionHistoryVersionManager sessionHistoryVersionManager;

        public string webServerRoot;
        private readonly object lockObject = new object();

        public SessionHistoryManager() : this(CoreUtil.APPLICATIONTEMPPATH) {
        }

        public SessionHistoryManager(string rootDirectory) {
            if (String.IsNullOrEmpty(rootDirectory)) {
                throw new Exception("root directory for session manager cannot be null/empty");
            }

            if (!Directory.Exists(rootDirectory)) {
                throw new Exception($"root directory for session manager must exist: {rootDirectory}");
            }

            webServerRoot = Path.Combine(rootDirectory, HttpSetup.WEB_PLUGIN_HOME);
            if (!Directory.Exists(webServerRoot)) {
                Directory.CreateDirectory(webServerRoot);
            }
        }

        public string StartNewSessionHistory(IProfileService profileService) {
            SessionHistory sessionHistory = new SessionHistory(DateTime.Now, profileService);
            sessionHistory.activeSession = true;
            string sessionHome = GetSessionHome(sessionHistory);
            string sessionJsonFile = Path.Combine(sessionHome, HttpSetup.SESSION_JSON_NAME);
            sessionHistoryVersionManager = new SessionHistoryVersionManager(sessionHistory, sessionJsonFile);
            return sessionHome;
        }

        public void Stop() {
            sessionHistoryVersionManager.Stop();
        }

        public void UpdateAddImageRecord(string sessionHome, string targetName, ImageRecord record) {
            lock (lockObject) {
                SessionHistory sessionHistory = sessionHistoryVersionManager.GetSessionHistory();
                string sessionJsonFile = Path.Combine(sessionHome, HttpSetup.SESSION_JSON_NAME);
                Target activeTarget = sessionHistory.GetActiveTarget();

                // Find the target to append to:
                // - If active target matches this name, use that
                // - Else if we already have a target with this name (even if not active), use that
                // - Otherwise create a new target

                if (activeTarget?.name != targetName) {
                    activeTarget = sessionHistory.GetTargetByName(targetName);
                    if (activeTarget == null) {
                        activeTarget = new Target(targetName);
                        sessionHistory.AddTarget(activeTarget);
                    }
                }

                activeTarget.AddImageRecord(record);
                sessionHistoryVersionManager.QueueVersion(sessionHistory, sessionJsonFile);
            }
        }

        public void UpdateAddEvent(string sessionHome, NINALogEvent logEvent) {
            lock (lockObject) {
                SessionHistory sessionHistory = sessionHistoryVersionManager.GetSessionHistory();
                string sessionJsonFile = Path.Combine(sessionHome, HttpSetup.SESSION_JSON_NAME);
                sessionHistory.AddLogEvent(logEvent);
                sessionHistoryVersionManager.QueueVersion(sessionHistory, sessionJsonFile);
            }
        }

        public void UpdateAddAutofocusEvent(string sessionHome, AutofocusEvent afEvent) {
            lock (lockObject) {
                SessionHistory sessionHistory = sessionHistoryVersionManager.GetSessionHistory();
                string sessionJsonFile = Path.Combine(sessionHome, HttpSetup.SESSION_JSON_NAME);
                sessionHistory.AddAutofocusEvent(afEvent);
                sessionHistoryVersionManager.QueueVersion(sessionHistory, sessionJsonFile);
            }
        }

        public void AddThumbnail(string sessionHome, string id, BitmapSource image) {
            string thumbnailFile = Path.Combine(sessionHome, HttpSetup.THUMBNAILS_ROOT, $"{id}.jpg");
            WriteThumbnail(thumbnailFile, image);
        }

        // TODO: we could be creating 'empty' session histories if the plugin isn't enabled.  Nice to just remove here (if no targets or events).
        public void PurgeHistoryOlderThan(int days) {
            days = days < 0 ? 0 : days;

            Logger.Info($"purging web session history older than {days} days");
            string[] sessionDirs = GetSessionHistoryDirectories();

            DateTime compareDate = DateTime.Now.AddDays(-1 * days);
            foreach (string dir in sessionDirs) {
                DateTime sessionDate = DateTime.ParseExact(Path.GetFileName(dir), "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                int diff = (sessionDate - compareDate).Days;
                if (diff <= 0) {
                    Logger.Info($"purging old web session history: {dir}");
                    Directory.Delete(dir, true);
                }
            }
        }

        public string GetSessionDirectoryName(SessionHistory sessionHistory) {
            return $"{sessionHistory.startTime.ToString("yyyyMMdd-HHmmss")}";
        }

        public SessionList GetSessionList() {
            string[] sessionDirs = GetSessionHistoryDirectories();
            SessionList sessionList = new SessionList();

            foreach (string dir in sessionDirs) {
                sessionList.AddSession(Path.GetFileName(dir));
            }

            return sessionList;
        }

        public void InitializeSessionList() {
            WriteSessionList(GetSessionList());
        }

        public void WriteSessionList(SessionList sessionList) {
            string sessionsListFile = Path.Combine(webServerRoot, HttpSetup.SESSIONS_ROOT, HttpSetup.SESSIONS_LIST_NAME);
            if (File.Exists(sessionsListFile)) {
                File.Delete(sessionsListFile);
            }

            sessionList.OrderSessions();
            JsonUtils.WriteJson(sessionList, sessionsListFile, true);
        }

        public void DeactivateOldSessions() {
            SessionList sessionList = GetSessionList();
            foreach (Session session in sessionList.sessions) {
                SessionHistory sessionHistory = ReadOldSessionHistory(session.key);
                if (sessionHistory.activeSession || sessionHistory.activeTargetId != null) {
                    Logger.Debug($"marking session inactive: {session.key}");
                    sessionHistory.activeSession = false;
                    sessionHistory.activeTargetId = null;
                    WriteOldSessionHistory(sessionHistory);
                }
            }
        }

        public string GetSessionHome(string sessionName) {
            return Path.Combine(webServerRoot, HttpSetup.SESSIONS_ROOT, sessionName);
        }

        private string GetSessionHome(SessionHistory sessionHistory) {
            string sessionHome = Path.Combine(webServerRoot, HttpSetup.SESSIONS_ROOT, GetSessionDirectoryName(sessionHistory));
            if (!Directory.Exists(sessionHome)) {
                Directory.CreateDirectory(sessionHome);
            }

            string thumbnailRoot = Path.Combine(sessionHome, HttpSetup.THUMBNAILS_ROOT);
            if (!Directory.Exists(thumbnailRoot)) {
                Directory.CreateDirectory(thumbnailRoot);
            }

            return sessionHome;
        }

        private string[] GetSessionHistoryDirectories() {
            string sessionsHome = Path.Combine(webServerRoot, HttpSetup.SESSIONS_ROOT);
            return Directory.GetDirectories(sessionsHome);
        }

        private SessionHistory ReadOldSessionHistory(string key) {
            string sessionJsonFile = Path.Combine(webServerRoot, HttpSetup.SESSIONS_ROOT, key, HttpSetup.SESSION_JSON_NAME);
            return JsonUtils.ReadJson<SessionHistory>(sessionJsonFile);
        }

        private string WriteOldSessionHistory(SessionHistory sessionHistory) {
            string sessionHome = GetSessionHome(sessionHistory);
            string sessionJsonFile = Path.Combine(sessionHome, HttpSetup.SESSION_JSON_NAME);

            string tempFile = Path.GetTempFileName();
            JsonUtils.WriteJson(sessionHistory, tempFile, true);

            if (File.Exists(sessionJsonFile)) {
                File.Delete(sessionJsonFile);
            }

            File.Move(tempFile, sessionJsonFile);
            return sessionHome;
        }

        private void WriteThumbnail(string thumbnailFile, BitmapSource imageSource) {
            // Cribbed from Lightbucket plugin (https://github.com/lightbucket-co/lightbucket-nina-plugin)
            double scaleFactor = 256 / imageSource.Width;
            BitmapSource resizedBitmap = new TransformedBitmap(imageSource, new ScaleTransform(scaleFactor, scaleFactor));
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 100;
            encoder.Frames.Add(BitmapFrame.Create(resizedBitmap));

            using (FileStream fs = new FileStream(thumbnailFile, FileMode.Create)) {
                encoder.Save(fs);
            }
        }
    }
}