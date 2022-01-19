using System;
using System.Collections.Generic;
using System.Globalization;

namespace Web.NINAPlugin.History {

    public class SessionList {
        public List<Session> sessions { get; set; }

        public SessionList() {
            sessions = new List<Session>();
        }

        public void AddSession(string key) {
            Session session = new Session();
            session.key = key;
            session.display = format(key);
            sessions.Add(session);
        }

        public void OrderSessions() {
            sessions.Sort(delegate (Session x, Session y) {
                if (x.key == null && y.key == null) return 0;
                else if (x.key == null) return -1;
                else if (y.key == null) return 1;
                else return x.key.CompareTo(y.key);
            });
        }

        private string format(string key) {
            DateTime dts = DateTime.ParseExact(key, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            return dts.ToString("MMM dd yyyy, HH:mm:ss");
        }
    }

    public class Session {
        public string key { get; set; }
        public string display { get; set; }

        public Session() {
        }
    }
}
