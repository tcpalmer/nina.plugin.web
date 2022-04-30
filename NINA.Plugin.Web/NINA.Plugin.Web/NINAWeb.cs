using NINA.Core.Utility;
using NINA.Image.Interfaces;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
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
        public const bool WebPluginEnabledDefault = false;
        public const int WebServerPortDefault = 80;
        public const int PurgeDaysDefault = 10;
        public const bool NonLightsDefault = false;

        private SessionHistoryManager sessionHistoryManager;
        private NINAEventWatcher eventWatcher;
        private AutofocusEventWatcher autofocusEventWatcher;
        private ImageSaveWatcher imageSaveWatcher;
        private IPluginOptionsAccessor pluginSettings;

        [ImportingConstructor]
        public WebPlugin(IProfileService profileService, IImageSaveMediator imageSaveMediator, IImageDataFactory imageDataFactory) {
            if (Properties.Settings.Default.UpdateSettings) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Properties.Settings.Default);
            }

            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            HttpServerInstance.SetImageDataFactory(imageDataFactory);

            bool initStatus = InitializePlugin(imageSaveMediator, profileService);
            if (!initStatus) {
                throw new Exception("failed to initialize the web session history viewer plugin");
            }
        }

        private bool InitializePlugin(IImageSaveMediator imageSaveMediator, IProfileService profileService) {
            try {
                setWebUrls();
                new HttpSetup().Initialize();

                sessionHistoryManager = new SessionHistoryManager();

                // Clean up existing session histories
                sessionHistoryManager.PurgeHistoryOlderThan(PurgeDays);
                sessionHistoryManager.RemoveEmptySessions();
                sessionHistoryManager.DeactivateOldSessions();

                this.imageSaveWatcher = new ImageSaveWatcher(imageSaveMediator, pluginSettings);
                this.eventWatcher = new NINAEventWatcher();
                this.autofocusEventWatcher = new AutofocusEventWatcher();

                // Create a new session history for this run
                string sessionHome = sessionHistoryManager.StartNewSessionHistory(profileService);
                imageSaveWatcher.setSessionHome(sessionHome);
                eventWatcher.setSessionHome(sessionHome);
                autofocusEventWatcher.setSessionHome(sessionHome);
                sessionHistoryManager.InitializeSessionList();

                if (WebPluginEnabled) {
                    HttpServerInstance.SetPort(WebServerPort);
                    HttpServerInstance.Start();
                    imageSaveWatcher.Start();
                    eventWatcher.Start();
                    autofocusEventWatcher.Start();
                }

                return true;
            }
            catch (Exception ex) {
                try {
                    imageSaveWatcher?.Stop();
                    eventWatcher?.Stop(false);
                    autofocusEventWatcher?.Stop();
                }
                catch (Exception) { }

                Logger.Error($"failed to initialize the web plugin: {ex} {ex.Source}, aborting");
                return false;
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(WebPluginEnabled));
            RaisePropertyChanged(nameof(PurgeDays));
            RaisePropertyChanged(nameof(WebServerPort));
            RaisePropertyChanged(nameof(NonLights));
        }

        public override Task Teardown() {
            try {
                HttpServerInstance.Stop();
                imageSaveWatcher.Stop();
                eventWatcher.Stop(true);
                autofocusEventWatcher.Stop();
                sessionHistoryManager.Stop();
            }
            catch (Exception ex) {
                Logger.Error($"failed to stop web server or event watchers at teardown time: {ex}");
            }

            return base.Teardown();
        }

        public bool WebPluginEnabled {
            get => pluginSettings.GetValueBoolean(nameof(WebPluginEnabled), WebPluginEnabledDefault);
            set {
                pluginSettings.SetValueBoolean(nameof(WebPluginEnabled), value);
                RaisePropertyChanged();
            }
        }

        public int WebServerPort {
            get => pluginSettings.GetValueInt32(nameof(WebServerPort), WebServerPortDefault);
            set {
                pluginSettings.SetValueInt32(nameof(WebServerPort), value);
                RaisePropertyChanged();
            }
        }

        public int PurgeDays {
            get => pluginSettings.GetValueInt32(nameof(PurgeDays), PurgeDaysDefault);
            set {
                pluginSettings.SetValueInt32(nameof(PurgeDays), value);
                RaisePropertyChanged();
            }
        }

        public bool NonLights {
            get => pluginSettings.GetValueBoolean(nameof(NonLights), NonLightsDefault);
            set {
                pluginSettings.SetValueBoolean(nameof(NonLights), value);
                RaisePropertyChanged();
            }
        }

        public string LocalAddress {
            get => Properties.Settings.Default.LocalAddress;
            set {
                Properties.Settings.Default.LocalAddress = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public string LocalNetworkAddress {
            get => Properties.Settings.Default.LocalNetworkAddress;
            set {
                Properties.Settings.Default.LocalNetworkAddress = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public string HostAddress {
            get => Properties.Settings.Default.HostAddress;
            set {
                Properties.Settings.Default.HostAddress = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        private void setWebUrls() {
            Dictionary<string, string> urls = new HttpServer(WebServerPort, null).GetURLs();
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

            switch (propertyName) {
                case "WebPluginEnabled":
                    if (WebPluginEnabled) {
                        HttpServerInstance.SetPort(WebServerPort);
                        HttpServerInstance.Start();
                        imageSaveWatcher.Start();
                        eventWatcher.Start();
                        autofocusEventWatcher.Start();
                    }
                    else {
                        HttpServerInstance.Stop();
                        imageSaveWatcher.Stop();
                        eventWatcher.Stop(false);
                        autofocusEventWatcher.Stop();
                    }
                    break;

                case "WebServerPort":
                    setWebUrls();
                    // Change the port, will auto-restart if already running
                    HttpServerInstance.SetPort(WebServerPort);
                    break;
            }
        }

        /* TODO: don't think this is used
        private void SettingsChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case "WebPluginEnabled":
                    if (WebPluginEnabled) {
                        HttpServerInstance.SetPort(WebServerPort);
                        HttpServerInstance.Start();
                        imageSaveWatcher.Start();
                        eventWatcher.Start();
                        autofocusEventWatcher.Start();
                    }
                    else {
                        HttpServerInstance.Stop();
                        imageSaveWatcher.Stop();
                        eventWatcher.Stop(false);
                        autofocusEventWatcher.Stop();
                    }
                    break;

                case "WebServerPort":
                    setWebUrls();
                    // Change the port, will auto-restart if already running
                    HttpServerInstance.SetPort(WebServerPort);
                    break;
            }
        }*/
    }
}