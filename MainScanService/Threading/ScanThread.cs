using System;
using System.Diagnostics;
using System.Threading;
using MainScanService.Helper;
using MainScanService.ClientWorker;

namespace MainScanService.Threading {
    class ScanThread {
        private readonly int resumeStep;
        protected IServiceExecution serviceExecution;
        protected Guid threadId;
        protected DateTime startTime;
        private bool breakOperation;

        private int serverCount = 5;

        public ScanThread(IServiceExecution serviceExecution, Guid threadId, int resumeStep = 0) {
            this.serviceExecution = serviceExecution;
            this.threadId = threadId;
            this.resumeStep = resumeStep;
        }

        public void Process(bool resumeProcessing = false) {
            startTime = DateTime.Now;

            try {
                if(resumeProcessing) {
                    ResumeProcessingData();
                } else {
                    ProcessingData();
                }
            } catch(Exception e) {
                EventLogger.Entry("Service Error: Thread crashed - "
                    + e, EventLogEntryType.Error);
            } finally {
                // Make sure, the thread gets closed
                // Add exeption-handling before, if needed!!!
                serviceExecution.ThreadFinished(threadId);
            }
        }

        public DateTime GetStartTime() {
            return startTime;
        }

        protected void ProcessingData() {
            Worker();
        }

        protected void ResumeProcessingData() {
            Worker();
        }

        public void BreakOperation() {
            breakOperation = true;
        }

        private void Worker() {
            Worker worker = new Worker(Properties.Settings.Default.server_list);
            if(breakOperation == true) {
                return;
            }
        }
    }
}
