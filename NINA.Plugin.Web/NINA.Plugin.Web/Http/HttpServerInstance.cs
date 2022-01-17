using System.Threading.Tasks;

namespace Web.NINAPlugin.Http {

    public sealed class HttpServerInstance {

        private static readonly object lockObject = new object();
        private static HttpServer instance = null;

        public static void Start() {
            lock (lockObject) {
                if (instance != null) {
                    instance.Stop();
                }

                instance = new HttpServer();
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
