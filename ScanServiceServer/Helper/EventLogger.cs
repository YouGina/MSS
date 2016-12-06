using System;
using System.Diagnostics;


namespace ScanServiceServer.Helper {
    public static class EventLogger {
        public static EventLog log { get; set; }

        public static void DebugEntry(string entry, EventLogEntryType type) {
            if(Properties.Settings.Default.enable_debug_log) {
                log.WriteEntry(DateTime.Now + " - " + entry, type);
            }
        }

        public static void Entry(string entry, EventLogEntryType type) {
            log.WriteEntry(DateTime.Now + " - " + entry, type);
        }
    }
}
