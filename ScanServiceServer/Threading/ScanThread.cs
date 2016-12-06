using System;
using System.Diagnostics;
using ScanServiceServer.Helper;
using ScanServiceServer.ClientWorker;
using System.Collections.Generic;
using System.IO;

namespace ScanServiceServer.Threading {
    class ScanThread {
        private readonly int resumeStep;
        protected IServiceExecution serviceExecution;
        protected Guid threadId;
        protected DateTime startTime;
        private bool breakOperation;

        public List<string> filesToScan = new List<string>();

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
                EventLogger.Entry("Service Error: Thread crashed - " + e, EventLogEntryType.Error);
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
            generateList(Properties.Settings.Default.default_path);
            List<List<string>> groupedFiles = splitList(filesToScan, (int)Math.Ceiling((double)(fileCount() / 8)));
            Worker w = new Worker(groupedFiles);
            if(breakOperation == true) {
                return;
            }
        }

        private void generateList(string path) {
            foreach(string f in Directory.GetFiles(path)) {
                filesToScan.Add(f);
            }
            foreach(string d in Directory.GetDirectories(path)) {
                generateList(d);
            }
        }

        public int fileCount() {
            return filesToScan.Count;
        }

        public static List<List<string>> splitList(List<string> locations, int nSize = 30) {
            List<List<string>> list = new List<List<string>>();

            for(int i = 0; i < locations.Count; i += nSize) {
                list.Add(locations.GetRange(i, Math.Min(nSize, locations.Count - i)));
            }

            return list;
        }
    }
}
