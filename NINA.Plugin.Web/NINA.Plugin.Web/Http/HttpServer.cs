using EmbedIO;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Image.Interfaces;
using Swan.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Web.NINAPlugin.Http.Api1020;

namespace Web.NINAPlugin.Http {

    public class HttpServer {

        public static readonly string HOST_KEY = "HostName";
        public static readonly string IP_KEY = "IPAddress";
        public static readonly string LOCALHOST_KEY = "LocalHost";

        private WebServer webServer;
        private CancellationTokenSource webServerToken;
        private Thread serverThread;
        private IImageDataFactory imageDataFactory;
        private int port;

        public HttpServer(int port, IImageDataFactory imageDataFactory) {
            this.imageDataFactory = imageDataFactory;
            this.port = port;
        }

        public Dictionary<string, string> GetURLs() {
            Dictionary<string, string> urls = new Dictionary<string, string>();
            Dictionary<string, string> names = GetLocalNames();
            string urlPort = port == 80 ? "" : $":{port}";

            foreach (KeyValuePair<string, string> entry in names) {
                urls.Add(entry.Key, $"http://{entry.Value}{urlPort}/{HttpSetup.WEB_CLIENT_DIR}");
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

            string localUrl = $"http://localhost:{port}/{HttpSetup.WEB_CLIENT_DIR}";
            NINA.Core.Utility.Logger.Info($"starting web server, listening at {localUrl}");

            try {
                using (webServer = CreateWebServer()) {
                    webServerToken = new CancellationTokenSource();
                    Notification.ShowSuccess($"Web server started, listening at {localUrl}");
                    webServer.RunAsync(webServerToken.Token).Wait();
                }
            }
            catch (Exception ex) {
                NINA.Core.Utility.Logger.Error($"failed to start web server: {ex}");
                Notification.ShowError($"Failed to start web server, see NINA log for details");

                NINA.Core.Utility.Logger.Debug("aborting web server thread");
                serverThread.Abort();
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

        private WebServer CreateWebServer() {
            string webClientPath = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, HttpSetup.WEB_PLUGIN_HOME);

            return new WebServer(o => o
                .WithUrlPrefix($"http://*:{this.port}")
                .WithMode(HttpListenerMode.EmbedIO))
                .WithWebApi("/api/1020", m => m
                    .WithController<ApiV1020Controller>(MyFactory))
                //.WithWebApi("/api/1020", m => m
                //   .WithController<ApiV1020Controller>())
                .WithStaticFolder("/", webClientPath, false);
        }

        private ApiV1020Controller MyFactory() {
            return new ApiV1020Controller(new ImageSupport(imageDataFactory));
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

        private Dictionary<string, string> GetLocalNames() {
            Dictionary<string, string> names = new Dictionary<string, string>();
            names.Add(LOCALHOST_KEY, "localhost");

            string hostName = Dns.GetHostName();
            if (!String.IsNullOrEmpty(hostName)) {
                NINA.Core.Utility.Logger.Debug($"host name: {hostName}");
                names.Add(HOST_KEY, hostName);
            }

            string ipv4 = GetIPv4Address();
            if (!String.IsNullOrEmpty(ipv4)) {
                names.Add(IP_KEY, ipv4);
            }

            return names;
        }

        private string GetIPv4Address() {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip.ToString();
                }
            }

            NINA.Core.Utility.Logger.Debug("no local IPv4 addresses found");
            return null;
        }
    }

}
