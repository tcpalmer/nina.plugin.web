using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Web.NINAPlugin.History;
using Web.NINAPlugin.Http.Api1020;

namespace Web.NINAPlugin.Http {

    public class ImageSupport {

        private IImageDataFactory imageDataFactory;

        public ImageSupport(IImageDataFactory imageDataFactory) {
            this.imageDataFactory = imageDataFactory;
        }

        public async Task<ImageMetaData> CreateWebImage(ImageStatusRequest request, string srcImageFile, string dstDirectory, string dstFileName) {
            IImageData imageData = await imageDataFactory.CreateFromFile(srcImageFile, 16, false, NINA.Core.Enum.RawConverterEnum.FREEIMAGE);
            IRenderedImage renderedImage = imageData.RenderImage();

            StretchOptions so = request.stretchOptions;
            renderedImage = await renderedImage.Stretch(so.autoStretchFactor, so.blackClipping, so.unlinkedStretch);
            BitmapSource bitmap = renderedImage.Image;

            if (request.imageScale != 1) {
                bitmap = new TransformedBitmap(bitmap, new ScaleTransform(request.imageScale, request.imageScale));
            }

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = request.qualityLevel;
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (FileStream fs = new FileStream(Path.Combine(dstDirectory, dstFileName), FileMode.Create)) {
                encoder.Save(fs);
            }

            return imageData.MetaData;
        }
    }
}
