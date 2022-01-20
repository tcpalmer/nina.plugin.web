using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Web;

namespace Web.NINAPlugin.History {

    public class SessionHistory {

        public string id { get; set; }
        public string pluginVersion { get; set; }
        public DateTime startTime { get; set; }
        public string activeTargetId { get; set; }
        public List<Target> targets { get; set; }

        public SessionHistory() {
            // for JSON deserialize
        }

        public SessionHistory(DateTime startTime) {
            id = Guid.NewGuid().ToString();
            pluginVersion = GetType().Assembly.GetName().Version.ToString();
            this.startTime = startTime;
            targets = new List<Target>();
        }

        public void AddTarget(Target target) {
            activeTargetId = target.id;
            targets.Add(target);
        }

        public Target GetActiveTarget() {
            if (activeTargetId == null) {
                return null;
            }

            foreach (Target t in targets) {
                if (t.id == activeTargetId) {
                    return t;
                }
            }

            throw new InvalidOperationException($"active target not found for id {activeTargetId}");
        }
    }

    public class Target {

        public string id { get; set; }
        public string name { get; set; }
        public DateTime startTime { get; set; }
        public List<ImageRecord> imageRecords { get; set; }

        public Target() { }

        public Target(string name) {
            id = Guid.NewGuid().ToString();
            this.name = name;
            startTime = DateTime.Now;
            imageRecords = new List<ImageRecord>();
        }

        public void AddImageRecord(ImageRecord record) {
            record.index = imageRecords.Count + 1;
            imageRecords.Add(record);
        }
    }

    public class ImageRecord {

        public string id { get; set; }
        public int index { get; set; }
        public string fileName { get; set; }
        public DateTime started { get; set; }
        public long epochMilliseconds { get; set; }
        public double duration { get; set; }
        public string filterName { get; set; }
        public int detectedStars { get; set; }
        public double HFR { get; set; }

        public ImageRecord() { }

        public ImageRecord(ImageSavedEventArgs msg) {
            id = Guid.NewGuid().ToString();
            fileName = GetFileName(msg.PathToImage);
            started = msg.MetaData.Image.ExposureStart;
            epochMilliseconds = new DateTimeOffset(started).ToUnixTimeMilliseconds();
            duration = msg.Duration;
            filterName = msg.Filter;
            detectedStars = msg.StarDetectionAnalysis.DetectedStars;
            HFR = msg.StarDetectionAnalysis.HFR;
        }

        private string GetFileName(Uri imageUri) {
            string path = HttpUtility.UrlDecode(imageUri.AbsolutePath);
            int pos = path.LastIndexOf('/');
            if (pos != -1) {
                return path.Substring(pos + 1);
            }

            return path;
        }
    }

}
