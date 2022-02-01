using NINA.Image.ImageData;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            return args;
        }
    }
}
