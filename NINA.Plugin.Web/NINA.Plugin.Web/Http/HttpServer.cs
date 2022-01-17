using EmbedIO;
using NINA.Core.Utility;
using Swan.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Web.NINAPlugin.Http {

    public class HttpServer {

        private WebServer webServer;
        private CancellationTokenSource webServerToken;
        private Thread serverThread;
        private string ip;
        private string port;

        public HttpServer() {
            ip = GetLocalIPAddress();
            port = "8080";
        }

        public void Start() {

            try {
                serverThread = new Thread(WebServerTask);
                serverThread.Name = "Web Server Thread";
                serverThread.Priority = ThreadPriority.BelowNormal;
                serverThread.Start();
            }
            catch (Exception ex) {
                NINA.Core.Utility.Logger.Error($"failed to start web server: {ex}");
            }
        }

        private void WebServerTask() {
            ConfigureLogging();

            string localUrlPrefix = $"http://localhost:{port}";
            string ipUrlPrefix = $"http://{ip}:{port}";
            NINA.Core.Utility.Logger.Info($"Starting web server listening at {localUrlPrefix} and {ipUrlPrefix}");

            using (webServer = CreateWebServer(localUrlPrefix, ipUrlPrefix)) {
                webServerToken = new CancellationTokenSource();
                webServer.RunAsync(webServerToken.Token).Wait();
            }
        }

        public void Stop() {
            NINA.Core.Utility.Logger.Debug("Stopping web server");
            try {
                if (webServer != null) {
                    if (webServer.State != WebServerState.Stopped) {
                        webServerToken.Cancel();
                        webServer.Dispose();
                        webServer = null;
                    }
                }

                if (serverThread != null && serverThread.IsAlive) {
                    serverThread.Abort();
                    serverThread = null;
                }
            }
            catch (Exception ex) {
                NINA.Core.Utility.Logger.Error($"failed to stop web server: {ex}");
            }
        }

        private WebServer CreateWebServer(string localUrlPrefix, string ipUrlPrefix) {
            // TODO: this is the location of the future web client files - so has to be properly under the installed 'Web Plugin'
            // following is correct
            // Location of Web client app files
            string webClientPath = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "WebPlugin");
            // TODO: follow is temp too - the 'WebPlugin' directory should be in place if installed
            if (!Directory.Exists(webClientPath)) {
                Directory.CreateDirectory(webClientPath);
            }

            return new WebServer(o => o
                    .WithUrlPrefix(localUrlPrefix)
                    .WithUrlPrefix(ipUrlPrefix)
                    .WithMode(HttpListenerMode.EmbedIO))
                    // TODO: will have to add static folder for JSON session history files dir
                    .WithStaticFolder("/", webClientPath, false);
                    //.WithStaticFolder("/", "C:\\Users\\Tom\\AppData\\Local\\NINA\\Plugins\\Web.NINAPlugin\\dist", false);
                    //.WithStaticFolder("/", webClientPath, false)

        }

        private void ConfigureLogging() {
            try { Swan.Logging.Logger.UnregisterLogger<ConsoleLogger>(); }
            catch (Exception) { }

            string logFileName = $"webserver-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.log";
            string logFilePath = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs", logFileName);
            NINA.Core.Utility.Logger.Info($"web server log: {logFilePath}");

            FileLogger fileLogger = new FileLogger(logFilePath, false) {
                LogLevel = LogLevel.Trace // TODO: make this a plugin option
            };

            Swan.Logging.Logger.RegisterLogger(fileLogger);
        }

        private string GetLocalIPAddress() {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip.ToString();
                }
            }

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
