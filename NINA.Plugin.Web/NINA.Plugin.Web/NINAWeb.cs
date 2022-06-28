using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Web.NINAPlugin.Autofocus;
using Web.NINAPlugin.History;
using Web.NINAPlugin.Http;

namespace Web.NINAPlugin {

    [Export(typeof(IPluginManifest))]
    public class WebPlugin : PluginBase, INotifyPropertyChanged {
        public const string WebPluginStateOFF = "OFF";
        public const string WebPluginStateON = "ON";
        public const string WebPluginStateSHARE = "SHARE";
        public const string WebPluginStateDefault = WebPluginStateOFF;

        public const int WebServerPortDefault = 80;
        public const int PurgeDaysDefault = 10;
        public const bool NonLightsDefault = false;

        private string WebPluginStateCurrent = WebPluginStateOFF;
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

                this.imageSaveWatcher = new ImageSaveWatcher(imageSaveMediator, pluginSettings);
                this.eventWatcher = new NINAEventWatcher();
                this.autofocusEventWatcher = new AutofocusEventWatcher();

                // Create a new session history for this run
                string sessionHome = sessionHistoryManager.StartNewSessionHistory(profileService);
                imageSaveWatcher.setSessionHome(sessionHome);
                eventWatcher.setSessionHome(sessionHome);
                autofocusEventWatcher.setSessionHome(sessionHome);
                sessionHistoryManager.InitializeSessionList();

                if (isPluginActive()) {
                    changePluginState();
                    WebPluginStateCurrent = WebPluginState;
                }

                return true;
            }
            catch (Exception ex) {
                try {
                    stopAllWatchers(false);
                }
                catch (Exception) { }

                Logger.Error($"failed to initialize the web plugin: {ex} {ex.Source}, aborting");
                return false;
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(WebPluginState));
            RaisePropertyChanged(nameof(PurgeDays));
            RaisePropertyChanged(nameof(WebServerPort));
            RaisePropertyChanged(nameof(NonLights));
        }

        public override Task Teardown() {
            try {
                HttpServerInstance.Stop();
                stopAllWatchers(true);
                sessionHistoryManager.Stop();
                if (lastNINARunning()) {
                    sessionHistoryManager.RemoveEmptySessions();
                    sessionHistoryManager.DeactivateOldSessions();
                }
            }
            catch (Exception ex) {
                Logger.Error($"failed to stop web server or event watchers at teardown time: {ex}");
            }

            return base.Teardown();
        }

        public IEnumerable<string> WebPluginStates {
            get {
                string[] states = { WebPluginStateOFF, WebPluginStateON, WebPluginStateSHARE };
                return Array.AsReadOnly(states);
            }
        }

        public string WebPluginState {
            get => pluginSettings.GetValueString(nameof(WebPluginState), WebPluginStateDefault);
            set {
                string current = pluginSettings.GetValueString(nameof(WebPluginState), WebPluginStateDefault);
                if (!value.Equals(current)) {
                    pluginSettings.SetValueString(nameof(WebPluginState), value);
                    RaisePropertyChanged();
                }
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

        private bool isPluginActive() {
            return WebPluginState.Equals(WebPluginStateON) || WebPluginState.Equals(WebPluginStateSHARE);
        }

        private void startAllWatchers() {
            imageSaveWatcher.Start();
            eventWatcher.Start();
            autofocusEventWatcher.Start();
        }

        private void stopAllWatchers(bool addStopEvent) {
            imageSaveWatcher.Stop();
            eventWatcher.Stop(addStopEvent);
            autofocusEventWatcher.Stop();
        }

        private bool lastNINARunning() {
            try {
                string ninaProcessName = Process.GetCurrentProcess().ProcessName;
                Process[] ninaProcesses = Process.GetProcessesByName(ninaProcessName);

                if (ninaProcesses.Length == 1) {
                    Logger.Debug("Last NINA running, Web viewer session clean up ...");
                    return true;
                }
                else {
                    Logger.Debug("Not the last NINA running, Web viewer skipping session clean up");
                    return false;
                }
            }
            catch (Exception e) {
                Logger.Warning($"exception determining running NINA instances: {e.Message}");
                return false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            switch (propertyName) {
                case nameof(WebPluginState):
                    changePluginState();
                    WebPluginStateCurrent = WebPluginState;
                    break;

                case nameof(WebServerPort):
                    setWebUrls();
                    // Change the port, will auto-restart if already running
                    HttpServerInstance.SetPort(WebServerPort);
                    break;
            }
        }

        private void changePluginState() {
            Logger.Info($"Web plugin state change from {WebPluginStateCurrent} to {WebPluginState}");

            /*
             * OFF -> ON:    start server and watchers
             * OFF -> SHARE: start watchers
             * ON  -> OFF:   stop server and watchers
             * ON  -> SHARE: stop server
             * SHARE -> OFF: stop watchers
             * SHARE -> ON:  start server
             */

            if (WebPluginStateCurrent.Equals(WebPluginStateOFF)) {
                // OFF -> ON: start server and watchers
                if (WebPluginState.Equals(WebPluginStateON)) {
                    HttpServerInstance.SetPort(WebServerPort);
                    HttpServerInstance.Start();
                    startAllWatchers();
                    Notification.ShowSuccess($"Web plugin in ON mode");
                    return;
                }

                // OFF -> SHARE: start watchers
                if (WebPluginState.Equals(WebPluginStateSHARE)) {
                    startAllWatchers();
                    Notification.ShowSuccess($"Web plugin in SHARE mode");
                    return;
                }
            }

            if (WebPluginStateCurrent.Equals(WebPluginStateON)) {
                // ON -> OFF: stop server and watchers
                if (WebPluginState.Equals(WebPluginStateOFF)) {
                    HttpServerInstance.Stop();
                    stopAllWatchers(false);
                    Notification.ShowSuccess($"Web plugin in OFF mode");
                    return;
                }

                // ON -> SHARE: stop server
                if (WebPluginState.Equals(WebPluginStateSHARE)) {
                    HttpServerInstance.Stop();
                    Notification.ShowSuccess($"Web plugin in SHARE mode");
                    return;
                }
            }

            if (WebPluginStateCurrent.Equals(WebPluginStateSHARE)) {
                // SHARE -> OFF: stop watchers
                if (WebPluginState.Equals(WebPluginStateOFF)) {
                    stopAllWatchers(false);
                    Notification.ShowSuccess($"Web plugin in OFF mode");
                    return;
                }

                // SHARE -> ON: start server
                if (WebPluginState.Equals(WebPluginStateON)) {
                    HttpServerInstance.SetPort(WebServerPort);
                    HttpServerInstance.Start();
                    Notification.ShowSuccess($"Web plugin in ON mode");
                    return;
                }
            }

            Logger.Error($"failed to switch Web Plugin state from current: {WebPluginStateCurrent} to new: {WebPluginState}");
        }
    }
}