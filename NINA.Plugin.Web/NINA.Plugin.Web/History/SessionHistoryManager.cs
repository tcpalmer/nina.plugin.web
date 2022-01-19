using Newtonsoft.Json;
using NINA.Core.Utility;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Web.NINAPlugin.Http;

namespace Web.NINAPlugin.History {

    public class SessionHistoryManager {

        public string webServerRoot;

        public SessionHistoryManager() : this(CoreUtil.APPLICATIONTEMPPATH) { }

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

        public SessionHistory GetSessionHistory(string sessionHome) {
            string sessionJsonFile = Path.Combine(sessionHome, HttpSetup.SESSION_JSON_NAME);
            string json = File.ReadAllText(sessionJsonFile);
            return JsonConvert.DeserializeObject<SessionHistory>(json);
        }

        public string CreateOrUpdateSessionHistory(SessionHistory sessionHistory) {
            string sessionHome = GetSessionHome(sessionHistory);
            string sessionJsonFile = Path.Combine(sessionHome, HttpSetup.SESSION_JSON_NAME);

            string tempFile = Path.GetTempFileName();
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Include;
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(tempFile))
            using (JsonWriter writer = new JsonTextWriter(sw)) {
                serializer.Serialize(writer, sessionHistory);
            }

            if (File.Exists(sessionJsonFile)) {
                File.Delete(sessionJsonFile);
            }

            File.Move(tempFile, sessionJsonFile);
            return sessionHome;
        }

        public void AddThumbnail(string sessionHome, string id, BitmapSource image) {
            string thumbnailFile = Path.Combine(sessionHome, HttpSetup.THUMBNAILS_ROOT, $"{id}.jpg");
            WriteThumbnail(thumbnailFile, image);
        }

        public void PurgeHistoryOlderThan(int days) {
            Logger.Debug($"purging session history older than {days} days");
            Logger.Warning("not yet purging old session history for web plugin");
            string sessionsRoot = Path.Combine(webServerRoot, HttpSetup.SESSIONS_ROOT);
            // TODO: load all subdirs of sessionsRoot matching yyyyMMdd-HHmmss, convert to date, purge if older than days
        }

        public string GetSessionDirectoryName(SessionHistory sessionHistory) {
            return $"{sessionHistory.startTime.ToString("yyyyMMdd-HHmmss")}";
        }

        public SessionList GetSessionList() {
            string sessionsHome = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, HttpSetup.WEB_PLUGIN_HOME, HttpSetup.SESSIONS_ROOT);
            string[] sessionDirs = Directory.GetDirectories(sessionsHome);
            SessionList sessionList = new SessionList();

            foreach (string dir in sessionDirs) {
                sessionList.AddSession(Path.GetFileName(dir));
            }

            return sessionList;
        }

        public void WriteSessionList(SessionList sessionList) {
            string sessionsListFile = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, HttpSetup.WEB_PLUGIN_HOME, HttpSetup.SESSIONS_ROOT, HttpSetup.SESSIONS_LIST_NAME);
            if (File.Exists(sessionsListFile)) {
                File.Delete(sessionsListFile);
            }

            sessionList.OrderSessions();

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Include;
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(sessionsListFile))
            using (JsonWriter writer = new JsonTextWriter(sw)) {
                serializer.Serialize(writer, sessionList);
            }
        }

        public void DeactivateSessions(SessionList sessionList) {
            foreach (Session session in sessionList.sessions) {
                SessionHistory sessionHistory = ReadSessionHistory(session.key);
                if (sessionHistory.activeTargetId != null) {
                    Logger.Debug($"marking session inactive: {session.key}");
                    sessionHistory.activeTargetId = null;
                    CreateOrUpdateSessionHistory(sessionHistory);
                }
            }
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

        private SessionHistory ReadSessionHistory(string key) {
            string sessionJsonFile = Path.Combine(webServerRoot, HttpSetup.SESSIONS_ROOT, key, HttpSetup.SESSION_JSON_NAME);
            string json = File.ReadAllText(sessionJsonFile);
            return JsonConvert.DeserializeObject<SessionHistory>(json);
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
