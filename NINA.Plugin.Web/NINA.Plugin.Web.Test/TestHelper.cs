using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using OxyPlot;
using System;
using System.Collections.Immutable;

namespace Web.NINAPlugin.Test {

    public class TestHelper {

        public static ImageSavedEventArgs GetImageSavedEventArgs(DateTime dateTime) {
            ImageMetaData metadata = new ImageMetaData();
            metadata.Image.ExposureStart = dateTime;

            ImageSavedEventArgs args = new ImageSavedEventArgs();
            args.MetaData = metadata;
            args.PathToImage = new Uri("file:///C:/foo/yoyo/bar.fits");
            args.Duration = 11.0;
            args.Filter = "Foo";
            args.StarDetectionAnalysis = new StarDetectionAnalysis();
            args.StarDetectionAnalysis.DetectedStars = 1234;
            args.StarDetectionAnalysis.HFR = 1.23;

            args.Statistics = new MockImageStatistics();

            return args;
        }
    }

    public class MockImageStatistics : IImageStatistics {
        public int BitDepth => 0;
        public double StDev => 0;
        public double Mean => 0;
        public double Median => 0;
        public double MedianAbsoluteDeviation => 0;
        public int Max => 0;
        public long MaxOccurrences => 0;
        public int Min => 0;
        public long MinOccurrences => 0;
        public ImmutableList<DataPoint> Histogram => throw new NotImplementedException();
    }
}