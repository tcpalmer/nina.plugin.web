using Newtonsoft.Json.Linq;
using NINA.Core.Utility;
using System;
using System.IO;

namespace Web.NINAPlugin.Autofocus {

    public class AutofocusEvent {
        public JRaw raw { get; set; }

        public AutofocusEvent() {
        }

        public AutofocusEvent(string fileName) {
            Logger.Debug($"web viewer loading AF file {fileName}");

            try {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var sr = new StreamReader(fs)) {
                    string json = sr.ReadToEnd();
                    this.raw = new JRaw(json);
                }
            }
            catch (Exception e) {
                Logger.Warning($"failed to read autofocus file for web viewer: {e.Message} {e.StackTrace}");
            }
        }
    }
}