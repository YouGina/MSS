using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using MainScanService.Threading;
using MainScanService.Helper;


namespace MainScanService {
    public enum State {
        Running,
        Shutting_Down,
        Stopped
    }

    class ServiceExecution : IServiceExecution {
        private ServiceExecution() { }
        private static ServiceExecution instance;
        private static readonly object instanceLock = new object();

        public IScanService scanService { get; set; }

        private State currentState = State.Stopped;

        private readonly Dictionary<Guid, ThreadHolder> runningThreads = new Dictionary<Guid, ThreadHolder>();

        private DateTime nextRun = DateTime.Now;

        private bool isProcessing;

        public static ServiceExecution GetInstance() {
            if(instance == null) {
                lock(instanceLock) {
                    if(instance == null) {
                        instance = new ServiceExecution();
                    }
                }
            }
            return instance;
        }

        public void StartServiceExecution() {
            try {
                currentState = State.Running;

                while(currentState == State.Running) {
                    EventLogger.DebugEntry("Main-Loop", EventLogEntryType.Information);
                    Thread.Sleep(10000);
                    CheckForRun();

                    EventLogger.DebugEntry("Main-Loop - Sleep", EventLogEntryType.Information);
                    Thread.Sleep(Properties.Settings.Default.interval * 1000 * 60);
                }


                while(currentState == State.Shutting_Down) {
                    using(LockHolder<Dictionary<Guid, ThreadHolder>> lockObj =
                        new LockHolder<Dictionary<Guid, ThreadHolder>>(runningThreads, 1000)) {
                        if(lockObj.LockSuccessful) {
                            foreach(ThreadHolder currentThread in runningThreads.Values) {
                                // Now break the processing of the complex thread
                                currentThread.scanThread.BreakOperation();
                            }

                            // If no more threads are left, set the state to stopped
                            if(runningThreads.Count == 0) {
                                currentState = State.Stopped;
                            }
                        }
                    }
                }
            } catch(Exception e) {
                EventLogger.Entry("Service Error: " + e + "\r\n\r\n" + e.StackTrace, EventLogEntryType.Error);
            }
        }

        private void CheckForRun() {
            if(isProcessing == false && nextRun <= DateTime.Now) {
                EventLogger.DebugEntry("Creating complex thread", EventLogEntryType.Information);

                ThreadHolder exportThread = new ThreadHolder();
                Guid guid = Guid.NewGuid();

                exportThread.scanThread = new ScanThread(this, guid);
                // Here you can add the resuming, on certain conditions
                // In my case I insert data to myThread which contains all the necessary objects to continue processing from the last halt
                // exportThread.myThread = new MyThreadComplex(this, guid, threadType, 3);

                if(CreateWorkerThread(exportThread, guid)) {
                    isProcessing = true;
                    EventLogger.DebugEntry("Creating complex thread - successful", EventLogEntryType.Information);
                } else {
                    EventLogger.Entry("Creating complex thread - failed", EventLogEntryType.Error);
                }
            }
        }

        private bool CreateWorkerThread(ThreadHolder exportThread, Guid guid) {
            using(LockHolder<Dictionary<Guid, ThreadHolder>> lockObj =
                new LockHolder<Dictionary<Guid, ThreadHolder>>(runningThreads, 1000)) {
                if(lockObj.LockSuccessful) {
                    Thread thread = new Thread(exportThread.Process) { Name = "ScanThread" };
                    exportThread.thread = thread;

                    runningThreads.Add(guid, exportThread);

                    thread.Start();

                    return true;
                }
            }

            return false;
        }

        public void ThreadFinished(Guid threadId) {
            EventLogger.DebugEntry("Thread closing start", EventLogEntryType.Information);

            using(LockHolder<Dictionary<Guid, ThreadHolder>> lockObj = new LockHolder<Dictionary<Guid, ThreadHolder>>(runningThreads, 1000)) {
                if(lockObj.LockSuccessful) {
                    nextRun = DateTime.Now.AddSeconds(Properties.Settings.Default.trigger_thread);
                    isProcessing = false;

                    runningThreads.Remove(threadId);
                }
            }

            EventLogger.DebugEntry("Thread closing end", EventLogEntryType.Information);
        }

        public void StopServiceExecution() {
            currentState = State.Shutting_Down;
        }


        public string GetCurrentThreadInfo() {
            EventLogger.DebugEntry("wcf ask for threads start", EventLogEntryType.Information);

            using(LockHolder<Dictionary<Guid, ThreadHolder>> lockObj =
                new LockHolder<Dictionary<Guid, ThreadHolder>>(runningThreads, 1000)) {
                if(lockObj.LockSuccessful) {
                    int counter = 0;
                    string threadInfo = "";

                    foreach(KeyValuePair<Guid, ThreadHolder> currentThread in runningThreads) {
                        DateTime tmpTime = currentThread.Value.scanThread.GetStartTime();

                        if(counter == 0) {
                            threadInfo = String.Format("{0:dd.MM.yyyy HH:mm:ss}", tmpTime) + "§" + "ScanThread";
                        } else {
                            threadInfo = threadInfo + "#" + String.Format("{0:dd.MM.yyyy HH:mm:ss}", tmpTime) + "§" + "ScanThread";
                        }

                        counter++;
                    }

                    EventLogger.DebugEntry("wcf ask for threads: " + threadInfo, EventLogEntryType.Information);

                    return threadInfo;
                }
            }

            return "";
        }
    }
}
