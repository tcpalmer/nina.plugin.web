using Newtonsoft.Json;
using NINA.Core.Utility;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Web.NINAPlugin.History {

    public class SessionHistoryManager {

        private static readonly string WEB_SERVER_ROOT = "WebPlugin";
        private static readonly string SESSIONS_ROOT = "sessions";
        private static readonly string THUMBNAILS_ROOT = "thumbnails";
        private static readonly string SESSION_JSON_NAME = "sessionHistory.json";

        /*
         * Directory structure:
         *   %localappdata\NINA\WebPlugin\%
         *     ws logs
         *     sessions\
         *       yyyyMMdd-HHmmss\
         *         sessionHistory.json
         *           thumbnails\
         *             <guid>.jpg
         * 
         */

        public string webServerRoot;

        public SessionHistoryManager() : this(CoreUtil.APPLICATIONTEMPPATH) { }

        public SessionHistoryManager(string rootDirectory) {

            if (String.IsNullOrEmpty(rootDirectory)) {
                throw new Exception("root directory for session manager cannot be null/empty");
            }

            if (!Directory.Exists(rootDirectory)) {
                throw new Exception($"root directory for session manager must exist: {rootDirectory}");
            }

            webServerRoot = Path.Combine(rootDirectory, WEB_SERVER_ROOT);
            if (!Directory.Exists(webServerRoot)) {
                Directory.CreateDirectory(webServerRoot);
            }
        }

        public SessionHistory GetSessionHistory(string sessionHome) {
            string sessionJsonFile = Path.Combine(sessionHome, SESSION_JSON_NAME);
            string json = File.ReadAllText(sessionJsonFile);
            return JsonConvert.DeserializeObject<SessionHistory>(json);
        }

        public string CreateOrUpdateSessionHistory(SessionHistory sessionHistory) {
            string sessionHome = GetSessionHome(sessionHistory);
            string sessionJsonFile = Path.Combine(sessionHome, SESSION_JSON_NAME);

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
            string thumbnailFile = Path.Combine(sessionHome, THUMBNAILS_ROOT, $"{id}.jpg");
            WriteThumbnail(thumbnailFile, image);
        }

        public void PurgeHistoryOlderThan(int days) {
            // TODO: delete session history dirs older than days
            string sessionsRoot = Path.Combine(webServerRoot, SESSIONS_ROOT);
            // TODO: load all subdirs of sessionsRoot matching yyyyMMdd-HHmmss, convert to date, purge if
        }

        private string GetSessionHome(SessionHistory sessionHistory) {
            string sessionHome = Path.Combine(webServerRoot, SESSIONS_ROOT, $"{sessionHistory.startTime.ToString("yyyyMMdd-HHmmss")}");
            if (!Directory.Exists(sessionHome)) {
                Directory.CreateDirectory(sessionHome);
            }

            string thumbnailRoot = Path.Combine(sessionHome, THUMBNAILS_ROOT);
            if (!Directory.Exists(thumbnailRoot)) {
                Directory.CreateDirectory(thumbnailRoot);
            }

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
