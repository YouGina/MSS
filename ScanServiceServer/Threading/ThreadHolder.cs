using System.Threading;

namespace ScanServiceServer.Threading {
    class ThreadHolder {
        public Thread thread { get; set; }
        public ScanThread scanThread { get; set; }
        public bool resuming { get; set; }

        public ThreadHolder() {
            resuming = false;
        }

        public void Process() {
            scanThread.Process(resuming);
        }
    }
}
