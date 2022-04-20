﻿using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Web.NINAPlugin.LogEvent {

    public class NINALogMessageProcessor : ININALogEventMediator {
        private static List<NINALogEvent> previousEvents = new List<NINALogEvent>();
        private Dictionary<Regex, EventMatcher> matchers;

        public NINALogMessageProcessor() {
            matchers = initMatchers();
        }

        public void processLogMessages(List<string> messages) {
            Logger.Trace($"processing {messages.Count} new log messages");

            foreach (var line in messages) {

                if (filterMessage(line)) {
                    continue;
                }

                string[] parts = line.Split('|');
                if (parts.Length == 6) {
                    string message = parts[5];

                    foreach (Regex regex in matchers.Keys) {
                        Match match = regex.Match(message);
                        if (match.Success) {
                            EventMatcher eventMatcher = matchers[regex];
                            DateTime dateTime = DateTime.Parse(parts[0]);
                            NINALogEvent logEvent = eventMatcher.handleMessage(eventMatcher, message, dateTime, match);
                            if (logEvent != null) {
                                onNINALogEvent(logEvent);
                            }
                        }
                    }
                }
                else {
                    Logger.Trace($"log message is not regular form: {line}, skipping");
                }
            }
        }

        public event EventHandler<NINALogEvent> NINALogEventSaved;

        public void onNINALogEvent(NINALogEvent e) {
            previousEvents.Add(e);
            Logger.Debug($"detected event for web viewer: {e.type}");
            NINALogEventSaved?.Invoke(this, e);
        }

        private Dictionary<Regex, EventMatcher> initMatchers() {
            Dictionary<Regex, EventMatcher> _matchers = new Dictionary<Regex, EventMatcher>();

            RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase;

            // Advanced sequence start
            Regex re = new Regex("^Advanced Sequence started$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_ADV_SEQ_START, false, null));

            // Advanced sequence finished
            re = new Regex("^Advanced Sequence finished$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_ADV_SEQ_STOP, false, null));

            // Dome opened
            re = new Regex("^Opened dome shutter\\. Shutter state after opening ShutterOpen$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_DOME_SHUTTER_OPENED, false, null));

            // Dome closed
            re = new Regex("^Closed dome shutter\\. Shutter state after closing ShutterClosed$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_DOME_SHUTTER_CLOSED, false, null));

            // Dome stopped
            re = new Regex("^Stopping all dome movement$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_DOME_STOPPED, false, null));

            // Unpark scope
            re = new Regex("^Telescope ordered to unpark$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_UNPARK, false, null));

            // Park scope
            re = new Regex("^Telescope has been commanded to park$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_PARK, false, null));

            // Center / Plate solve
            re = new Regex("^Starting Category: Telescope, Item: Center, (?<extra>.+)$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_CENTER, true, null));

            // Slew - don't think we really need slews
            //re = new Regex("^Slewing from (?<extra>.+)$", options);
            //_matchers.Add(re, new EventMatcher(NINALogEvent.NINA_SLEW, true, EventMatcher.handleSlewEvent));

            // Meridian flip
            re = new Regex("^Meridian Flip - Initializing Meridian Flip.+", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_MF, false, null));

            // Error: Auto Focus
            re = new Regex("^Auto Focus Failed! (?<extra>.+)$", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_ERROR_AF, true, null));

            // Error: Plate solve
            re = new Regex("^ASTAP - Plate solve failed.", options);
            _matchers.Add(re, new EventMatcher(NINALogEvent.NINA_ERROR_PLATESOLVE, false, null));

            return _matchers;
        }

        private bool filterMessage(string line) {
            if (line?.Length == 0) {
                return true;
            }

            if (line.Contains("|DEBUG|") || line.Contains("|TRACE|")) {
                return true;
            }

            return false;
        }

        public delegate NINALogEvent HandleMessageDelegate(EventMatcher eventMatcher, string msg, DateTime dateTime, Match match);

        public class EventMatcher {
            public string eventType { get; }
            public bool hasExtra { get; }
            public HandleMessageDelegate customMessageHandler;

            public EventMatcher(string eventType, bool hasExtra, HandleMessageDelegate handleMessage) {
                this.eventType = eventType;
                this.hasExtra = hasExtra;
                this.customMessageHandler = handleMessage;
            }

            public NINALogEvent handleMessage(EventMatcher eventMatcher, string msg, DateTime dateTime, Match match) {
                if (customMessageHandler != null) {
                    return customMessageHandler(eventMatcher, msg, dateTime, match);
                }

                if (eventMatcher.hasExtra && match.Groups.Count > 0) {
                    return new NINALogEvent(eventMatcher.eventType, dateTime, match.Groups["extra"].Value);
                }
                else {
                    return new NINALogEvent(eventMatcher.eventType, dateTime);
                }
            }

            // Currently unused since we're not handling slews as events
            public static NINALogEvent handleSlewEvent(EventMatcher eventMatcher, string msg, DateTime dateTime, Match match) {
                // Need to scan back into previous events and see if there's a plate solve or MF w/in some constrained amount of time (like 3m)
                // If there is, then return null - we don't want to record slews of plate solves/MFs.
                // Looks like previousEvents.FindLastIndex() could do this w/out explicitly walking back

                if (eventMatcher.hasExtra && match.Groups.Count > 0) {
                    return new NINALogEvent(eventMatcher.eventType, dateTime, match.Groups["extra"].Value);
                }
                else {
                    return new NINALogEvent(eventMatcher.eventType);
                }
            }
        }
    }
}