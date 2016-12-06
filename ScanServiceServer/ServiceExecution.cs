using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using ScanServiceServer.Threading;
using ScanServiceServer.Helper;
using System.Net.Sockets;
using System.Net;


namespace ScanServiceServer {
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
        
        private bool isProcessing;

        private Socket s;
        private TcpListener myList;

        private bool connectionActive = false;

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

        public IPAddress LocalIPAddress() {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach(IPAddress ip in host.AddressList) {
                if(ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip;
                }
            }
            return null;
        }

        public void StartServiceExecution() {
            try {
                currentState = State.Running;

                while(currentState == State.Running) {
                    if(!connectionActive) { 
                        connectionActive = true;
                        EventLogger.DebugEntry("Main-Loop", EventLogEntryType.Information);
                        IPAddress ipAd = IPAddress.Parse(Properties.Settings.Default.IPAddress);
                        myList = new TcpListener(ipAd, 19358);

                        myList.Start();

                        s = myList.AcceptSocket();

                        byte[] b = new byte[100];
                        int k = s.Receive(b);

                        if(compareBytes(b, GetBytes("startScan", 100))) {
                            CheckForRun();
                        }
                    }
                    EventLogger.DebugEntry("Main-Loop - Sleep", EventLogEntryType.Information);
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
                EventLogger.Entry("Service Error: " + e, EventLogEntryType.Error);
            }
        }

        public void sendResultsToClient(String results, bool done = false) {
            s.Send(GetBytes(results, 512));
            if(done) {
                s.Send(GetBytes("done", 512));
                s.Close();
                myList.Stop();
                connectionActive = false;
            }
        }

        private void CheckForRun() {
            if(isProcessing == false) {
                EventLogger.DebugEntry("Creating complex thread", EventLogEntryType.Information);

                ThreadHolder exportThread = new ThreadHolder();
                Guid guid = Guid.NewGuid();

                exportThread.scanThread = new ScanThread(this, guid);

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

        bool compareBytes(byte[] a, byte[] b) {
            bool ret = true;
            if(a.Length != b.Length) {
                ret = false;
            }
            for(int i = 0; i < a.Length; i++) {
                if(a[i] != b[i]) {
                    ret = false;
                }
            }
            return ret;
        }
        public byte[] GetBytes(string str, int size) {
            byte[] bytes = new byte[size];
            for(int i = 0; i < str.Length; i++) {
                bytes[i] = Convert.ToByte(str[i]);
            }
            return bytes;
        }
    }

}
