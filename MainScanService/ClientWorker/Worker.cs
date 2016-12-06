using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Diagnostics;
using MainScanService.Helper;

namespace MainScanService.ClientWorker {
    
    class Worker {
        private const int maxWorkersRunning = 8;
        private int workerCounter = 0;
        private int runningWorkers = 0;
        private int lastWorkerStarted = -1;
        private int maxWorker;

        
        public Worker(StringCollection servers) {
            try {
                EventLogger.DebugEntry("Service Start: Service started", EventLogEntryType.SuccessAudit);
                List<String> list = new List<String>();
                list = servers.Cast<String>().ToList();
                maxWorker = list.Count;
                for(int i = 0; i < maxWorkersRunning && i < list.Count; i++) {
                    BackgroundWorker bw = startWorker();
                    bw.RunWorkerAsync(list[++lastWorkerStarted]);
                }
            } catch(Exception ex) {
                EventLogger.Entry("!Service Error: Worker error, could not start first itteration!;  " + ex.StackTrace + " \r\n\r\nServers count: "+servers.Count, EventLogEntryType.Error);
            }
        }

        private BackgroundWorker startWorker() {
            workerCounter++;
            runningWorkers++;

            BackgroundWorker bw = new BackgroundWorker();

            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = false;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

            return bw;
        }


        private void bw_DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker worker = sender as BackgroundWorker;
            String value = (String)e.Argument;
            try {
                List<String> res = new List<String>();
                TcpClient tcpclnt = new TcpClient();

                tcpclnt.Connect(value, 19358);
                // use the ipaddress as in the server program

                String str = "startScan";
                Stream stm = tcpclnt.GetStream();

                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] ba = asen.GetBytes(str);

                stm.Write(ba, 0, ba.Length);
                byte[] bb = new byte[512];
                int k = 0;
                String tmpString = "";
                do {
                    k = stm.Read(bb, 0, 512);
                    tmpString = new String(asen.GetChars(bb));
                    tmpString = tmpString.Replace("\0", string.Empty);
                    res.Add(tmpString);
                } while(tmpString != "done");

                tcpclnt.Close();
                e.Result = res;                
            } catch(Exception ex) {
                EventLogger.Entry("Service Error: TCP Client connect error " + ex.StackTrace + "\r\n" + ex.Message + "\r\n\r\n"+ex.Data+"\r\n\r\ncurrentWorker: "+ workerCounter, EventLogEntryType.Error);
            }

        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            Console.WriteLine(e.ProgressPercentage.ToString() + "%");
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            runningWorkers -= 1;
            List<String> res = (List<String>)e.Result;
            if(!(e.Error == null)) {
                EventLogger.Entry("Service Error: !" + e.Error.Message + "\r\n\r\n" + e.Error.StackTrace, EventLogEntryType.Error);
            } else {
                EventLogger.DebugEntry("Service Information: Done!", EventLogEntryType.Information);
                // send mail
                SendMail m = new SendMail();
                m.generateReport(res);
                res.Clear();
            }
            while(runningWorkers < 8 && workerCounter < maxWorker) {
                BackgroundWorker newWorker = startWorker();
                newWorker.RunWorkerAsync(++lastWorkerStarted);
            }
            BackgroundWorker bw = (BackgroundWorker)sender;
            bw.Dispose();
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
            for(int i = 0; i < str.Length && i <= size; i++) {
                bytes[i] = Convert.ToByte(str[i]);
            }
            return bytes;
        }
    }
}
