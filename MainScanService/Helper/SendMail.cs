using System.Net;
using System.Net.Mail;

using System.Collections.Generic;

using MainScanService.Helper;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace MainScanService.Helper {
    class SendMail {
        private SmtpClient smtpClient = new SmtpClient();
        private NetworkCredential basicCredential;
        private MailAddress fromAddress;

        private String User = "no-reply@mijnpartnergroep.nl";
        private String Password = "x!6!Zqk*8HFP";


        public SendMail() {
            try {

                basicCredential = new NetworkCredential(User, Password, Properties.Settings.Default.mail_server);
                fromAddress = new MailAddress(User);

                smtpClient.Host = Properties.Settings.Default.mail_server;
                smtpClient.Port = Properties.Settings.Default.mail_port;
                smtpClient.EnableSsl = false;
                
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = basicCredential;
                smtpClient.Timeout = (60 * 5 * 1000);

            } catch(Exception e) {
                EventLogger.Entry("Mail error: " + e.Message, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        public void generateReport(List<String> files) {
            String report = "";
            Dictionary<String, List<String>> listPerUser = new Dictionary<String, List<String>>();
            foreach(String x in files) {
                String path = x.Replace("E:\\HostingSpaces\\", "");
                String[] f = path.Split(new String[] { "\\" }, StringSplitOptions.None);
                if(listPerUser.ContainsKey(f[0])) {
                    listPerUser[f[0]].Add(path.Replace(f[0], ""));
                } else {
                    List<String> t = new List<String>();
                    if(f[0].Length > 0) { 
                        t.Add(path.Replace(f[0], ""));
                        listPerUser.Add(f[0], t);
                    }
                }
            }
            if(listPerUser.Count > 1) { 
                foreach(KeyValuePair<String, List<String>> dict in listPerUser) {
                    if(dict.Key != "done") { 
                        report += "<h3>Infected files of: " + dict.Key + "</h3>";
                        foreach(String p in dict.Value) {
                            report += p + "<br />";
                        }
                        report += "<br /><br />";
                    }
                }
            }
            List<String> sendTo = Properties.Settings.Default.send_to.Cast<String>().ToList();
            MailMessage message = new MailMessage();
            message.From = fromAddress;
            message.Subject = "Rapport - " + DateTime.Now.Date.ToString().Split(' ')[0];
            message.IsBodyHtml = true;
            message.Body = report;
            foreach(String address in sendTo) {
                message.To.Add(address);
            }
            smtpClient.Send(message);
        }
    }
}
