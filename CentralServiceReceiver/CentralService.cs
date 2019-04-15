using System;
using System.Configuration;
using System.IO;
using System.Threading;
using Common;
using Common.Models.Properties;
using Common.ServiceBus;

namespace CentralServiceReceiver
{
    public class CentralService
    {
        private readonly ManualResetEvent _stopEvent;
        private readonly FileSystemWatcher _watcher;
        private readonly ServerSbClient _sbClient;
        private readonly string _cnString;
        private readonly string _infoQueueName;

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            ConfigurationManager.RefreshSection("appSettings");
            var barcodeText = ConfigurationManager.AppSettings["barcodeText"];
            var scanTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["scanTimeout"]);
            _sbClient.SendPropertiesUpdate(new Properties(scanTimeout, barcodeText));
            _sbClient.SendStateRequest();
        }

        public void StartScanner()
        {
            _watcher.EnableRaisingEvents = true;
        }
        
        public CentralService(string output, string cnString, string infoQueueName, string stateQueueName)
        {
            if (!Directory.Exists(output))
                Directory.CreateDirectory(output);

            _stopEvent = new ManualResetEvent(false);

            _sbClient = new ServerSbClient(output, cnString, infoQueueName);
            _sbClient.CreateInfoListener(infoQueueName);
            _sbClient.InitTopics();

            _watcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory);
            _watcher.Changed += Watcher_Changed;
            _watcher.Filter = "*.exe.config";
        }

        public bool Start()
        {
            var myThread = new Thread(StartScanner) { IsBackground = true };
            myThread.Start();
            return true;
        }

        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
            _stopEvent.Set();
        }
    }
}