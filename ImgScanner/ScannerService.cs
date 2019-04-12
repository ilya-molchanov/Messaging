using System.Threading;
using Common.Models.Properties;

namespace ImgScanner
{
    public class ScannerService
    {
        private readonly Scanner _scanner;

        public ScannerService(ScanProperties props)
        {
            _scanner = new Scanner(props);
        }

        public bool Start()
        {
            var myThread = new Thread(_scanner.Start) { IsBackground = true };
            myThread.Start();
            return true;
        }

        public void Stop()
        {
            _scanner.Stop();
        }
    }
}
