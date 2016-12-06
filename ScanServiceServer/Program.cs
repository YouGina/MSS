﻿using System;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

namespace ScanServiceServer {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args) {
            if(Environment.UserInteractive) {
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
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { 
                    new ScanServiceServer() 
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
