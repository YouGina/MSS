using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanServiceServer {
    interface IScanService {
        void StartService();

        void StopService();
    }
}
