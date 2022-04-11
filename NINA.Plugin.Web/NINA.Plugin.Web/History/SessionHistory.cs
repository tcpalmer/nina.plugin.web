using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Web;
using Web.NINAPlugin.Autofocus;
using Web.NINAPlugin.LogEvent;

namespace Web.NINAPlugin.History {

    public class SessionHistory {
        public string id { get; set; }
        public string pluginVersion { get; set; }
        public int sessionVersion { get; set; }
        public DateTime startTime { get; set; }
        public bool activeSession { get; set; }
        public string activeTargetId { get; set; }
        public StretchOptions stretchOptions { get; set; }
        public List<NINALogEvent> events { get; set; }
        public List<AutofocusEvent> autofocus { get; set; }
        public List<Target> targets { get; set; }

        public SessionHistory() {
            // for JSON deserialize
        }

        public SessionHistory(DateTime startTime, IProfileService profileService) {
            id = Guid.NewGuid().ToString();
            pluginVersion = GetType().Assembly.GetName().Version.ToString();
            sessionVersion = 0;
            this.startTime = startTime;
            this.stretchOptions = new StretchOptions(profileService);
            events = new List<NINALogEvent>();
            autofocus = new List<AutofocusEvent>();
            targets = new List<Target>();
        }

        public void AddLogEvent(NINALogEvent logEvent) {
            events.Add(logEvent);
        }

        public void AddAutofocusEvent(AutofocusEvent afEvent) {
            autofocus.Add(afEvent);
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

        public Target GetTargetByName(string targetName) {
            foreach (Target t in targets) {
                if (t.name == targetName) {
                    return t;
                }
            }

            return null;
        }
    }

    public class StretchOptions {
        public double autoStretchFactor { get; set; }
        public double blackClipping { get; set; }
        public bool unlinkedStretch { get; set; }

        public StretchOptions() {
        }

        public StretchOptions(IProfileService profileService) {
            autoStretchFactor = profileService.ActiveProfile.ImageSettings.AutoStretchFactor;
            blackClipping = profileService.ActiveProfile.ImageSettings.BlackClipping;
            unlinkedStretch = profileService.ActiveProfile.ImageSettings.UnlinkedStretch;
        }
    }

    public class Target {
        public string id { get; set; }
        public string name { get; set; }
        public DateTime startTime { get; set; }
        public List<ImageRecord> imageRecords { get; set; }

        public Target() {
        }

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
        public string fullPath { get; set; }
        public DateTime started { get; set; }
        public long epochMilliseconds { get; set; }
        public double duration { get; set; }
        public string filterName { get; set; }
        public int detectedStars { get; set; }
        public double HFR { get; set; }
        public double ADUStDev { get; set; }
        public double ADUMean { get; set; }
        public double ADUMedian { get; set; }
        public int ADUMin { get; set; }
        public int ADUMax { get; set; }
        public double ADUMAD { get; set; }
        public double GuidingRMS { get; set; }
        public double GuidingRMSArcSec { get; set; }
        public double GuidingRMSRA { get; set; }
        public double GuidingRMSRAArcSec { get; set; }
        public double GuidingRMSDEC { get; set; }
        public double GuidingRMSDECArcSec { get; set; }

        public ImageRecord() {
        }

        public ImageRecord(ImageSavedEventArgs msg) {
            id = Guid.NewGuid().ToString();
            fileName = GetFileName(msg.PathToImage);
            fullPath = HttpUtility.UrlDecode(msg.PathToImage.AbsolutePath);

            started = msg.MetaData.Image.ExposureStart;
            epochMilliseconds = new DateTimeOffset(started).ToUnixTimeMilliseconds();
            duration = msg.Duration;
            filterName = msg.Filter;

            detectedStars = msg.StarDetectionAnalysis.DetectedStars;
            HFR = msg.StarDetectionAnalysis.HFR;

            ADUStDev = msg.Statistics.StDev;
            ADUMean = msg.Statistics.Mean;
            ADUMedian = msg.Statistics.Mean;
            ADUMin = msg.Statistics.Min;
            ADUMax = msg.Statistics.Max;
            ADUMAD = msg.Statistics.MedianAbsoluteDeviation;

            GuidingRMS = GetGuidingMetric(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.Total);
            GuidingRMSArcSec = GetGuidingMetricArcSec(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.Total);
            GuidingRMSRA = GetGuidingMetric(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.RA);
            GuidingRMSRAArcSec = GetGuidingMetricArcSec(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.RA);
            GuidingRMSDEC = GetGuidingMetric(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.Dec);
            GuidingRMSDECArcSec = GetGuidingMetricArcSec(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.Dec);
        }

        private double GetGuidingMetric(ImageParameter image, double? metric) {
            return (image.RecordedRMS != null && metric != null) ? ReformatDouble((double)metric) : 0.0;
        }

        private double GetGuidingMetricArcSec(ImageParameter image, double? metric) {
            return (image.RecordedRMS != null && metric != null) ? ReformatDouble((double)(metric * image.RecordedRMS.Scale)) : 0.0;
        }

        public static Double ReformatDouble(Double value) {
            return Double.Parse(String.Format("{0:0.####}", value));
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