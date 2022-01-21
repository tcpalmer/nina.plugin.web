using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Plugin;
using System;
using System.IO;
using Web.NINAPlugin.History;

namespace Web.NINAPlugin.Http {

    public class HttpSetup {

        static public readonly string PLUGIN_HOME = "Web Session History Viewer";
        static public readonly string WEB_PLUGIN_HOME = "WebPlugin";

        static public readonly string WEB_CLIENT_DIR = "dist";
        static private readonly string WEB_CLIENT_VERSION_FILE = "webClientVersion.json";

        static public readonly string SESSIONS_ROOT = "sessions";
        static public readonly string SESSIONS_LIST_NAME = "sessions.json";
        static public readonly string THUMBNAILS_ROOT = "thumbnails";
        static public readonly string SESSION_JSON_NAME = "sessionHistory.json";

        /*
         * Directory structure:
         *   %localappdata\NINA\WebPlugin\%
         *     dist\
         *       <web client files>
         *       webClientVersion.json
         *     sessions\
         *       sessions.json
         *       yyyyMMdd-HHmmss\
         *         sessionHistory.json
         *           thumbnails\
         *             <guid>.jpg
         */

        public void Initialize() {

            // The plugin name as installed by NINA under 'Plugins'
            string pluginName = PLUGIN_HOME;
            Logger.Debug($"plugin name: {pluginName}");

            string pluginVersion = GetType().Assembly.GetName().Version.ToString();
            Logger.Debug($"plugin version: {pluginVersion}");

            // Confirm we can locate our plugin installation
            string pluginHome = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, Constants.UserExtensionsFolder, pluginName);
            Logger.Debug($"plugin home: {pluginHome}");
            if (!Directory.Exists(pluginHome)) {
                throw new Exception($"failed to locate plugin home: {pluginHome} not found");
            }

            // Create the web server home if not present
            string webServerHome = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, WEB_PLUGIN_HOME);
            if (!Directory.Exists(webServerHome)) {
                Logger.Debug($"creating web server home: {webServerHome}");
                Directory.CreateDirectory(webServerHome);
            }

            // Create the web sessions home if not present
            string sessionsHome = Path.Combine(webServerHome, SESSIONS_ROOT);
            if (!Directory.Exists(sessionsHome)) {
                Logger.Debug($"creating web sessions home: {sessionsHome}");
                Directory.CreateDirectory(sessionsHome);
            }

            // If no session list exists, create an empty one
            string sessionListFile = Path.Combine(sessionsHome, SESSIONS_LIST_NAME);
            if (!File.Exists(sessionListFile)) {
                new SessionHistoryManager().WriteSessionList(new SessionList());
            }

            // If the web client files aren't present, or the version differs, install
            string webClientFiles = Path.Combine(webServerHome, WEB_CLIENT_DIR);
            string webClientVersionFile = Path.Combine(webClientFiles, WEB_CLIENT_VERSION_FILE);
            if (!File.Exists(webClientVersionFile)) {
                Logger.Debug($"web client missing, installing");
                InstallWebClient(pluginHome, webServerHome);
            }
            else {
                // Compare installed version with plugin version
                WebClientVersion installedWebClientVersion = GetWebClientVersion(webClientVersionFile);
                Logger.Debug($"installed web client version: {installedWebClientVersion.version}");

                string pluginWebClientVersionFile = Path.Combine(pluginHome, WEB_CLIENT_DIR, WEB_CLIENT_VERSION_FILE);
                WebClientVersion pluginWebClientVersion = GetWebClientVersion(pluginWebClientVersionFile);
                Logger.Debug($"plugin web client version: {pluginWebClientVersion.version}");

                if (installedWebClientVersion.version != pluginWebClientVersion.version) {
                    Logger.Debug("installed web client differs, re-installing");
                    InstallWebClient(pluginHome, webServerHome);
                }
                else {
                    Logger.Debug("target web client version already installed");
                }
            }
        }

        private void InstallWebClient(string pluginHome, string webServerHome) {
            string srcDir = Path.Combine(pluginHome, WEB_CLIENT_DIR);
            string dstDir = Path.Combine(webServerHome, WEB_CLIENT_DIR);

            if (Directory.Exists(dstDir)) {
                Logger.Debug($"removing existing web client directory: {dstDir}");
                Directory.Delete(dstDir, true);
            }

            CopyDirectory(srcDir, dstDir, true);
        }

        private WebClientVersion GetWebClientVersion(string fileName) {
            string json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<WebClientVersion>(json);
        }

        private void CopyDirectory(string srcDir, string dstDir, bool recursive) {
            var dir = new DirectoryInfo(srcDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(dstDir);

            foreach (FileInfo file in dir.GetFiles()) {
                string targetFilePath = Path.Combine(dstDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (recursive) {
                foreach (DirectoryInfo subDir in dirs) {
                    string newDestinationDir = Path.Combine(dstDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }

    public class WebClientVersion {
        public string version { get; set; }

        public WebClientVersion() {
        }
    }
}
