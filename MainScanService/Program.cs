using System;
using System.Diagnostics;
using System.ServiceProcess;
using MainScanService.Helper;
using System.Configuration.Install;
using System.Reflection;

namespace MainScanService {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args) {
            try {
                if(System.Environment.UserInteractive) {
                    if(args.Length > 0) {
                        switch(args[0]) {
                            case "-install": {
                                    ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                                    break;
                                }
                            case "-uninstall":
                                ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                                break;
                        }
                    }
                } else {
                    ServiceBase[] ServicesToRun =
				{ 
					new MainScanService() 
				};
                    ServiceBase.Run(ServicesToRun);
                }
            } catch(Exception e) {
                EventLogger.Entry("Service Error:  - Main crashed - " + e, EventLogEntryType.Error);
            }
        }
    }
}
