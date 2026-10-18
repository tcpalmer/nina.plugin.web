using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Core.Utility;
using System;
using System.IO;
using System.Threading;

namespace Web.NINAPlugin.Autofocus {

    public class AutofocusEvent {
        private const int MAX_READ_ATTEMPTS = 5;
        private const int READ_RETRY_DELAY_MS = 250;

        public JRaw raw { get; set; }

        public AutofocusEvent() {
        }

        public AutofocusEvent(string fileName) {
            Logger.Debug($"web viewer loading AF file {fileName}");

            // NINA may still be writing the file when we're notified, so it can be locked or only partially written
            for (int attempt = 1; attempt <= MAX_READ_ATTEMPTS; attempt++) {
                try {
                    using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs)) {
                        string json = sr.ReadToEnd();
                        JToken.Parse(json);
                        this.raw = new JRaw(json);
                        return;
                    }
                }
                catch (Exception e) when (e is IOException || e is JsonReaderException) {
                    if (attempt == MAX_READ_ATTEMPTS) {
                        Logger.Warning($"failed to read autofocus file for web viewer: {e.Message} {e.StackTrace}");
                        return;
                    }

                    Logger.Debug($"web viewer AF file not ready (attempt {attempt}): {e.Message}");
                    Thread.Sleep(READ_RETRY_DELAY_MS);
                }
                catch (Exception e) {
                    Logger.Warning($"failed to read autofocus file for web viewer: {e.Message} {e.StackTrace}");
                    return;
                }
            }
        }
    }
}