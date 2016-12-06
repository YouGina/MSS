using ScanServiceServer.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScanServiceServer {
    public partial class ScanServiceServer : ServiceBase, IScanService {
        private Thread thread { get; set; }

        private bool finalExit { get; set; }

        public ScanServiceServer() {
            try { 
                InitializeComponent();

                finalExit = true;

                if(!EventLog.SourceExists("ScanServiceServer")) {
                    EventLog.CreateEventSource("ScanServiceServer", "");
                }

                ScanServiceServerEventLog.Source = "ScanServiceServer";
                ScanServiceServerEventLog.Log = "";

                ServiceExecution.GetInstance().scanService = this;

                EventLogger.log = ScanServiceServerEventLog;

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
