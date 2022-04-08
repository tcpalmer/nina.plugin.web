using Newtonsoft.Json.Linq;

namespace Web.NINAPlugin.Autofocus {

    public class AutofocusEvent {
        public JRaw raw { get; set; }

        public AutofocusEvent() {
        }

        public AutofocusEvent(string autofocusJson) {
            this.raw = new JRaw(autofocusJson);
        }
    }
}