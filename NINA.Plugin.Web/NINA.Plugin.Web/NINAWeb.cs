using NINA.Core.Utility;
using NINA.Image.Interfaces;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Web.NINAPlugin.Autofocus;
using Web.NINAPlugin.History;
using Web.NINAPlugin.Http;
using Web.NINAPlugin.Properties;

namespace Web.NINAPlugin {

    [Export(typeof(IPluginManifest))]
    public class WebPlugin : PluginBase, INotifyPropertyChanged {
        private SessionHistoryManager sessionHistoryManager;
        private NINAEventWatcher eventWatcher;
        private AutofocusEventWatcher autofocusEventWatcher;
        private ImageSaveWatcher imageSaveWatcher;

        [ImportingConstructor]
        public WebPlugin(IProfileService profileService, IImageSaveMediator imageSaveMediator, IImageDataFactory imageDataFactory) {
            if (Settings.Default.UpdateSettings) {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }

            Settings.Default.PropertyChanged += SettingsChanged;

            this.eventWatcher = new NINAEventWatcher();
            this.autofocusEventWatcher = new AutofocusEventWatcher();
            this.imageSaveWatcher = new ImageSaveWatcher(imageSaveMediator);

            HttpServerInstance.SetImageDataFactory(imageDataFactory);
            InitializePlugin(profileService);
        }

        private void InitializePlugin(IProfileService profileService) {
            try {
                setWebUrls();
                new HttpSetup().Initialize();

                sessionHistoryManager = new SessionHistoryManager();

                // Clean up existing session histories
                sessionHistoryManager.PurgeHistoryOlderThan(Settings.Default.PurgeDays);
                sessionHistoryManager.DeactivateOldSessions();

                // Create a new session history for this run
                string sessionHome = sessionHistoryManager.StartNewSessionHistory(profileService);
                eventWatcher.setSessionHome(sessionHome);
                imageSaveWatcher.setSessionHome(sessionHome);
                autofocusEventWatcher.setSessionHome(sessionHome);
                sessionHistoryManager.InitializeSessionList();

                if (WebPluginEnabled) {
                    HttpServerInstance.SetPort(WebServerPort);
                    HttpServerInstance.Start();
                    eventWatcher.Start();
                    autofocusEventWatcher.Start();
                }
            }
            catch (Exception ex) {
                Logger.Error($"failed to initialize the web plugin: {ex} {ex.Source}, aborting");
                return;
            }
        }

        public override Task Teardown() {
            try {
                HttpServerInstance.Stop();
                eventWatcher.Stop();
                autofocusEventWatcher.Stop();
                sessionHistoryManager.Stop();
            }
            catch (Exception ex) {
                Logger.Error($"failed to stop web server or event watchers at teardown time: {ex}");
            }

            return base.Teardown();
        }

        public bool WebPluginEnabled {
            get => Settings.Default.WebPluginEnabled;
            set {
                Settings.Default.WebPluginEnabled = value;
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public int WebServerPort {
            get => Settings.Default.WebServerPort;
            set {
                Settings.Default.WebServerPort = value;
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public int PurgeDays {
            get => Settings.Default.PurgeDays;
            set {
                Settings.Default.PurgeDays = value;
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public bool NonLights {
            get => Settings.Default.NonLights;
            set {
                Settings.Default.NonLights = value;
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public string LocalAddress {
            get => Settings.Default.LocalAddress;
            set {
                Settings.Default.LocalAddress = value;
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public string LocalNetworkAddress {
            get => Settings.Default.LocalNetworkAddress;
            set {
                Settings.Default.LocalNetworkAddress = value;
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public string HostAddress {
            get => Settings.Default.HostAddress;
            set {
                Settings.Default.HostAddress = value;
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        private void setWebUrls() {
            Dictionary<string, string> urls = new HttpServer(Settings.Default.WebServerPort, null).GetURLs();
            LocalAddress = urls[HttpServer.LOCALHOST_KEY];

            if (urls.ContainsKey(HttpServer.IP_KEY)) {
                LocalNetworkAddress = urls[HttpServer.IP_KEY];
            }

            if (urls.ContainsKey(HttpServer.HOST_KEY)) {
                HostAddress = urls[HttpServer.HOST_KEY];
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SettingsChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case "WebPluginEnabled":
                    if (Settings.Default.WebPluginEnabled) {
                        HttpServerInstance.SetPort(Settings.Default.WebServerPort);
                        HttpServerInstance.Start();
                        eventWatcher.Start();
                        autofocusEventWatcher.Start();
                    }
                    else {
                        HttpServerInstance.Stop();
                        eventWatcher.Stop();
                        autofocusEventWatcher.Stop();
                    }
                    break;

                case "WebServerPort":
                    setWebUrls();
                    // Change the port, will auto-restart if already running
                    HttpServerInstance.SetPort(Settings.Default.WebServerPort);
                    break;
            }
        }
    }
}