using ScanServiceServer.FileScanWorker;
using ScanServiceServer.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace ScanServiceServer.ClientWorker {
    
    class Worker {

        List<string> infectedFiles = new List<string>();
        int allDone = 0;
        int toDo;
        public Worker(List<List<string>> list) {
            toDo = list.Count;
            foreach(List<string> files in list) {
                BackgroundWorker bw = startWorker();
                bw.RunWorkerAsync(files);
            }
        }

        private BackgroundWorker startWorker() {
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = false;
            bw.WorkerSupportsCancellation = false;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            return bw;
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e) {
            try { 
                BackgroundWorker worker = sender as BackgroundWorker;
                List<string> files = (List<string>)e.Argument;
                List<string> possibleInfected = Properties.Settings.Default.EvilStrings.Cast<string>().ToList();
                // Regex malicious = new Regex(@"(mail|eval|base64_decode|str_rot13|chmod|fwrite|exec|passthru|shell_exec|system|proc_open|popen|curl_exec|curl_multi_exec|show_source|fsockopen|pfsockopen|stream_socket_client) *\(");
                foreach(string file in files) {
                    if(MaliciousHashes.checkHash(file.create(true))) {
                        infectedFiles.Add(file);
                    }
                    string evilString = "";
                    int evilLine = 0;
                    IEnumerable<string> lines = System.IO.File.ReadLines(file);
                    foreach(string line in lines) {
                        evilLine++;
                        if(possibleInfected.Any(x => { evilString = x;
                           return line.Contains(x);
                        })) {
                            infectedFiles.Add(file + " <-- <b>Found possible evil string ("+evilString+" on line: "+evilLine+")</b>");
                        }
                        //foreach(Match match in malicious.Matches(line)) { 
                        //    infectedFiles.Add(file + " <-- <b>Found possible evil string (" + match.Value + " on line: " + evilLine + ")</b>");
                        //}
                    }
                }
            } catch(Exception ex) {
                EventLogger.Entry("Service Error: " + ex.Message + "\r\n\r\n"+ ex.StackTrace + "\r\n\r\n" + ex.Source, EventLogEntryType.Error);
            }
            allDone++;
        }


        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if(!(e.Error == null)) {
                Console.WriteLine("Error: " + e.Error.Message);
            } else {
                if(allDone == toDo) {
                    foreach(string file in infectedFiles) { 
                        ServiceExecution.GetInstance().sendResultsToClient(file);
                    }
                    ServiceExecution.GetInstance().sendResultsToClient("done", true);
                }
            }
        }
    }
}
