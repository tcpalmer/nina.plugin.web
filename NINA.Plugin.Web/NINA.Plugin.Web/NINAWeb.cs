using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
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

            try {
                new HttpSetup().Initialize();
            }
            catch (Exception ex) {
                Logger.Error($"failed to initialize the web client: {ex}");
                return;
            }

            HttpServerInstance.Start();
            new ImageSaveWatcher(imageSaveMediator);
        }


        public override Task Teardown() {
            HttpServerInstance.Stop();
            return base.Teardown();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
