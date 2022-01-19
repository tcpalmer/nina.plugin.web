namespace Web.NINAPlugin.Http {

    public sealed class HttpServerInstance {

        private static readonly object lockObject = new object();
        private static HttpServer instance = null;
        private static int serverPort;

        public static void SetPort(int port) {
            if (port != serverPort) {
                serverPort = port;
                if (instance != null) {
                    Start();
                }
            }
        }

        public static void Start() {
            lock (lockObject) {
                if (instance != null) {
                    instance.Stop();
                }

                instance = new HttpServer(serverPort);
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
