using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Web.NINAPlugin.Http;
using Web.NINAPlugin.Properties;

namespace Web.NINAPlugin {

    [Export(typeof(IPluginManifest))]
    public class WebPlugin : PluginBase, INotifyPropertyChanged {

        [ImportingConstructor]
        public WebPlugin(IImageSaveMediator imageSaveMediator) {
            if (Settings.Default.UpdateSettings) {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }

            Settings.Default.PropertyChanged += SettingsChanged;
            setWebUrls();

            try {
                new HttpSetup().Initialize();
                if (WebPluginEnabled) {
                    HttpServerInstance.SetPort(WebServerPort);
                    HttpServerInstance.Start();
                }
            }
            catch (Exception ex) {
                Logger.Error($"failed to initialize the web server: {ex}, aborting");
                return;
            }

            new ImageSaveWatcher(imageSaveMediator);
        }

        public override Task Teardown() {
            HttpServerInstance.Stop();
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

        private void setWebUrls() {
            List<string> urls = new HttpServer(Settings.Default.WebServerPort).GetURLs();
            LocalAddress = urls.ElementAt(0);
            LocalNetworkAddress = urls.Count > 1 ? urls.ElementAt(1) : null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void SettingsChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {

                case "WebPluginEnabled":
                    if (Settings.Default.WebPluginEnabled) {
                        HttpServerInstance.Start();
                    }
                    else {
                        HttpServerInstance.Stop();
                    }
                    break;

                case "WebServerPort":
                    setWebUrls();
                    HttpServerInstance.SetPort(Settings.Default.WebServerPort);
                    break;
            }
        }
    }
}
