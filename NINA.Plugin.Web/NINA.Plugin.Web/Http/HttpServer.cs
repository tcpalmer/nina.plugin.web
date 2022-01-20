using EmbedIO;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using Swan.Logging;
using System;
using System.Collections.Generic;
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
        private int port;

        public HttpServer(int port) {
            ip = GetLocalIPAddress();
            this.port = port;
        }

        public List<string> GetURLs() {
            List<string> urls = new List<string>();
            string urlPort = port == 80 ? "" : $":{port}";
            urls.Add($"http://localhost{urlPort}/{HttpSetup.WEB_CLIENT_DIR}");

            if (ip != null) {
                urls.Add($"http://{ip}{urlPort}/{HttpSetup.WEB_CLIENT_DIR}");
            }

            return urls;
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
            string localUrl = $"{localUrlPrefix}/{HttpSetup.WEB_CLIENT_DIR}";

            string ipUrlPrefix = null;
            string ipUrl = null;

            if (ip != null) {
                ipUrlPrefix = $"http://{ip}:{port}";
                ipUrl = $"{ipUrlPrefix}/{HttpSetup.WEB_CLIENT_DIR}";
                NINA.Core.Utility.Logger.Info($"starting web server, listening at {localUrl} and {ipUrl}");
            }
            else {
                NINA.Core.Utility.Logger.Info($"starting web server, listening at {localUrl}");
            }

            using (webServer = CreateWebServer(localUrlPrefix, ipUrlPrefix)) {
                webServerToken = new CancellationTokenSource();

                if (ip != null) {
                    Notification.ShowSuccess($"Web server started, listening at {localUrl} and {ipUrl}");
                }
                else {
                    Notification.ShowSuccess($"Web server started, listening at {localUrl}");
                }

                webServer.RunAsync(webServerToken.Token).Wait();
            }
        }

        public void Stop() {
            NINA.Core.Utility.Logger.Debug("stopping web server");
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

                Notification.ShowSuccess($"Web server stopped");
            }
            catch (Exception ex) {
                NINA.Core.Utility.Logger.Error($"failed to stop web server: {ex}");
            }
        }

        private WebServer CreateWebServer(string localUrlPrefix, string ipUrlPrefix) {
            string webClientPath = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, HttpSetup.WEB_PLUGIN_HOME);

            if (ipUrlPrefix != null) {
                return new WebServer(o => o
                        .WithUrlPrefix(localUrlPrefix)
                        .WithUrlPrefix(ipUrlPrefix)
                        .WithMode(HttpListenerMode.EmbedIO))
                        .WithStaticFolder("/", webClientPath, false);
            }
            else {
                return new WebServer(o => o
                        .WithUrlPrefix(localUrlPrefix)
                        .WithMode(HttpListenerMode.EmbedIO))
                        .WithStaticFolder("/", webClientPath, false);
            }
        }

        private void ConfigureLogging() {
            try { Swan.Logging.Logger.UnregisterLogger<ConsoleLogger>(); }
            catch (Exception) { }

            string logFileName = $"webserver-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.log";
            string logFilePath = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, HttpSetup.WEB_PLUGIN_HOME, logFileName);
            NINA.Core.Utility.Logger.Info($"web server log: {logFilePath}");

            FileLogger fileLogger = new FileLogger(logFilePath, false) {
                LogLevel = LogLevel.Info
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

            NINA.Core.Utility.Logger.Warning("failed to find local IP address");
            return null;
        }
    }
}
