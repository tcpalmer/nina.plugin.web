using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Plugin;
using System;
using System.IO;

namespace Web.NINAPlugin.Http {

    public class HttpSetup {

        static private readonly string WEB_PLUGIN_HOME = "WebPlugin";
        static private readonly string WEB_CLIENT_DIR = "dist";
        static private readonly string WEB_CLIENT_VERSION_FILE = "webClientVersion.json";

        public void Initialize() {

            // The plugin name as installed by NINA under 'Plugins' comes from the plugin manifest
            // which is pulled from our AssemblyTitle when the manifest is created at plugin package time.
            string pluginName = GetType().Assembly.GetName().Name;
            Logger.Debug($"plugin name: {pluginName}");

            string pluginVersion = GetType().Assembly.GetName().Version.ToString();
            Logger.Debug($"plugin version: {pluginVersion}");

            // Confirm we can locate our plugin home
            string pluginHome = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, Constants.UserExtensionsFolder, pluginName);
            Logger.Debug($"plugin home: {pluginHome}");
            if (!Directory.Exists(pluginHome)) {
                throw new Exception($"failed to locate plugin home: {pluginHome} not found, aborting");
            }

            // Create the web server home if not present
            string webServerHome = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, WEB_PLUGIN_HOME);
            if (!Directory.Exists(webServerHome)) {
                Logger.Debug($"creating web server home: {webServerHome}");
                Directory.CreateDirectory(webServerHome);
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
