using NINA.Image.Interfaces;

namespace Web.NINAPlugin.Http {

    public sealed class HttpServerInstance {

        private static readonly object lockObject = new object();
        private static HttpServer instance = null;
        private static int serverPort;
        private static IImageDataFactory imageDataFactory;

        public static void SetPort(int port) {
            if (port != serverPort) {
                serverPort = port;
                if (instance != null) {
                    Start();
                }
            }
        }

        public static void SetImageDataFactory(IImageDataFactory factory) {
            imageDataFactory = factory;
        }

        public static void Start() {
            lock (lockObject) {
                if (instance != null) {
                    instance.Stop();
                }

                instance = new HttpServer(serverPort, imageDataFactory);
                instance.Start();
            }
        }

        public static void Stop() {
            lock (lockObject) {
                if (instance != null) {
                    instance.Stop();
                    instance = null;
                }
            }
        }

        private HttpServerInstance() { }
    }
}
