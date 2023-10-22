using NINA.Core.Utility;
using System;
using System.IO;

namespace Web.NINAPlugin.Utility {

    public class LogUtils {

        public static string GetLogDirectory() {
            return Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs");
        }

        public static string GetLogFileRE() {
            // NINA log files match yyyyMMdd-HHmmss-VERSION.PID-yyyyMMdd.log - see NINA.Core.Utility.Logger.
            // Note that daily log file rolling was added in Sept 2023 - that added the '-yyyyMMdd' to the end.

            string version = CoreUtil.Version;
            int processId = Environment.ProcessId;
            return @"^\d{8}-\d{6}-" + $"{version}.{processId}" + @"-\d{8}.log$";
        }

        private LogUtils() { }
    }
}
