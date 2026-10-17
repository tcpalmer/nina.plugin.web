using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;

namespace Web.NINAPlugin.Autofocus {

    public class AutoFocuserConsumer : IFocuserConsumer {

        public void Dispose() {
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
            Logger.Info($"UpdateDeviceInfo: {deviceInfo.DisplayName}");
        }

        public void UpdateEndAutoFocusRun(AutoFocusInfo info) {
            Logger.Info($"UpdateEndAutoFocusRun: {info.Position}");
        }

        public void UpdateUserFocused(FocuserInfo info) {
            Logger.Info($"UpdateUserFocused: {info.Name}");
        }
    }
}