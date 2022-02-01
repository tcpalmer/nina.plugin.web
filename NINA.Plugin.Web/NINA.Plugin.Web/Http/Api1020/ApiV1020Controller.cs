using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Web.NINAPlugin.History;
using Web.NINAPlugin.Utility;

namespace Web.NINAPlugin.Http.Api1020 {

    public class ApiV1020Controller : WebApiController {

        private SessionHistoryManager sessionHistoryManager;
        private ImageSupport imageSupport;

        public ApiV1020Controller(ImageSupport imageSupport) {
            this.sessionHistoryManager = new SessionHistoryManager();
            this.imageSupport = imageSupport;
        }

        [Route(HttpVerbs.Put, "/image/create")]
        public async Task<ImageStatus> ImageStatus() {

            try {
                ImageStatusRequest request = await HttpContext.GetRequestDataAsync<ImageStatusRequest>();

                Logger.Debug($"API 1020: /image/create: {request.sessionName}, {request.id}, {request.fullPath}");

                string cacheDirectory = GetImageCacheDirectory(request.sessionName);
                string cacheKey = $"{request.id}-{JsonUtils.GetHashCode(request)}";

                // Check the cache to see if we've already handled this request
                ImageStatus imageStatus = GetImageStatus(cacheDirectory, cacheKey);
                if (imageStatus != null) {
                    Logger.Debug($"found existing image status for cache key {cacheKey}");
                    return imageStatus;
                }

                // Confirm that the original image still exists
                string imagePath = Path.GetFullPath(request.fullPath);
                if (!File.Exists(imagePath)) {
                    Logger.Warning($"can't convert image for web view, original not found: {imagePath}");
                    throw new HttpException(HttpStatusCode.NotFound, "original image missing");
                }

                // Create the Web-ready copy of the image
                string urlPath = await CreateWebReadyImage(cacheDirectory, cacheKey, request, imagePath);

                // Create the status, cache it, and return
                imageStatus = new ImageStatus();
                imageStatus.id = request.id;
                imageStatus.urlPath = urlPath;
                imageStatus.cached = DateTime.Now;

                PutImageStatus(cacheDirectory, cacheKey, imageStatus);
                return imageStatus;
            }
            catch (Exception ex) {
                Logger.Warning($"error in web view API /image/create: {ex}");

                if (ex is HttpException) {
                    throw ex;
                }

                throw new HttpException(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private string GetImageCacheDirectory(string sessionName) {
            string sessionDirectory = sessionHistoryManager.GetSessionHome(sessionName);
            if (!Directory.Exists(sessionDirectory)) {
                throw new HttpException(System.Net.HttpStatusCode.BadRequest, $"session home not found: {sessionDirectory}");
            }

            // Create it if not present
            string cacheDirectory = Path.Combine(sessionDirectory, HttpSetup.IMAGES_ROOT);
            if (!Directory.Exists(cacheDirectory)) {
                Directory.CreateDirectory(cacheDirectory);
            }

            return cacheDirectory;
        }

        private ImageStatus GetImageStatus(string cacheDirectory, string cacheKey) {
            string statusFile = Path.Combine(cacheDirectory, $"{cacheKey}.json");
            if (!File.Exists(statusFile)) {
                return null;
            }

            return JsonUtils.ReadJson<ImageStatus>(statusFile);
        }

        private async Task<string> CreateWebReadyImage(string cacheDirectory, string cacheKey, ImageStatusRequest request, string srcImagePath) {
            string imageFileName = $"{cacheKey}.jpg";
            ImageMetaData imageMetaData = await imageSupport.CreateWebImage(request, srcImagePath, cacheDirectory, imageFileName);
            // TODO: do something with the metadata
            return $"/{HttpSetup.SESSIONS_ROOT}/{request.sessionName}/{HttpSetup.IMAGES_ROOT}/{cacheKey}.jpg";
        }

        private void PutImageStatus(string cacheDirectory, string cacheKey, ImageStatus imageStatus) {
            string statusFile = Path.Combine(cacheDirectory, $"{cacheKey}.json");
            JsonUtils.WriteJson(imageStatus, statusFile);
        }

    }

    public class ImageStatusRequest {
        public string sessionName { get; set; }
        public string id { get; set; }
        public string fullPath { get; set; }
        public StretchOptions stretchOptions { get; set; }
        public double imageScale { get; set; }
        public int qualityLevel { get; set; }

        public ImageStatusRequest() {
        }
    }

    public class ImageStatus {
        public string id { get; set; }
        public DateTime cached { get; set; }
        public string urlPath { get; set; }
        // TODO: other metadata

        public ImageStatus() {
        }
    }

}