using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using MainScanService.Helper;

namespace MainScanService {
    public partial class MainScanService : ServiceBase, IScanService {
        private Thread thread { get; set; }

        private bool finalExit { get; set; }

        public MainScanService() {
            try { 
                InitializeComponent();

                finalExit = true;

                if(!EventLog.SourceExists("MainScanService")) {
                    EventLog.CreateEventSource("MainScanService", "");
                }

                MainScanServiceEventLog.Source = "MainScanService";
                MainScanServiceEventLog.Log = "";

                ServiceExecution.GetInstance().scanService = this;

                EventLogger.log = MainScanServiceEventLog;

                // ServiceExecution.GetInstance().myService = this;
            } catch(Exception e) {
                EventLogger.Entry("Service Error: Init - " + e, EventLogEntryType.Error);
            }
        }

        protected override void OnStart(string[] args) {
            try {
                thread = new Thread(ServiceExecution.GetInstance().StartServiceExecution) {
                    Name = "Service Executer"
                };
                thread.Start();
                EventLogger.DebugEntry("Service started", EventLogEntryType.Information);
            } catch(Exception e) {
                EventLogger.Entry("Service Error: Starting - " + e, EventLogEntryType.Error);
            }
        }

        protected override void OnStop() {
            try {
                ServiceExecution.GetInstance().StopServiceExecution();
                thread.Join();
                EventLogger.DebugEntry("Service stopped", EventLogEntryType.Information);
            } catch(Exception e) {
                EventLogger.Entry("Service Error: Stopping - " + e, EventLogEntryType.Error);
            }
        }

        public void StartService() {
            OnStart(null);
        }

        public void StopService() {
            finalExit = false;
            OnStop();
        }
    }
}
