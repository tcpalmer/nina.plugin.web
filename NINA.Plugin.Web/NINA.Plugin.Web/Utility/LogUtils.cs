using NINA.Core.Utility;
using System;
using System.IO;

namespace Web.NINAPlugin.Utility {

    public class LogUtils {

        public static string GetLogDirectory() {
            return Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs");
        }

        public static string GetLogFileRE() {
            // NINA log files match yyyyMMdd-HHmmss-VERSION.PID-yyyyMM.log - see NINA.Core.Utility.Logger.
            // Note that daily log file rolling was added in Sept 2023 - that added the '-yyyyMMdd' to the end.
            // It was then switched to monthly rolling in Oct 2023 so now '-yyyyMM' but RE will match either.

            string version = CoreUtil.Version;
            int processId = Environment.ProcessId;
            return @"^\d{8}-\d{6}-" + $"{version}.{processId}" + @"-\d{6,8}.log$";
        }

        private LogUtils() {
        }
    }
}