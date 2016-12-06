using System;
using System.IO;
using System.Text;

namespace ScanServiceServer.FileScanWorker {
    public static class MD5 {
        public static String create(this String str, bool file) {
            using(var md5 = System.Security.Cryptography.MD5.Create()) {
                if(file) {
                    using(var stream = File.OpenRead(str)) {
                        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                    }
                } else {
                    return BitConverter.ToString(md5.ComputeHash(Encoding.ASCII.GetBytes(str))).Replace("-", "").ToLower();
                }
            }
        }
    }
}