using System;
using System.Configuration;
using System.IO;
using Common.Models.Properties;
using NLog;
using NLog.Config;
using NLog.Targets;
using Topshelf;

namespace ImgScanner
{
    internal class Program
    {
        private static void Main()
        {
            var inputFolder = ConfigurationManager.AppSettings["input"];
            var errorsFolder = ConfigurationManager.AppSettings["errors"];
            var cnString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
            var infoQueueName = ConfigurationManager.AppSettings["infoQueueName"];
            var stateQueueName = ConfigurationManager.AppSettings["stateQueueName"];
            var scanTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["scanTimeout"]);
            var stateInterval = Convert.ToInt32(ConfigurationManager.AppSettings["stateInterval"]);
            var barcodeText = ConfigurationManager.AppSettings["barcodeText"];

            var props = new ScanProperties(inputFolder, errorsFolder, infoQueueName, stateQueueName, cnString, scanTimeout, stateInterval, barcodeText);

            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            var logConfig = new LoggingConfiguration();
            var target = new FileTarget
            {
                Name = "Def",
                FileName = logPath,
                Layout = "${date} ${message} ${onexception:inner=${exception:format=toString}}"
            };

            logConfig.AddTarget(target);
            logConfig.AddRuleForAllLevels(target);

            var logFactory = new LogFactory(logConfig);
            var currentLogger = logFactory.GetLogger("Topshelf");
            Logger.Logger.SetLogger(currentLogger);

            HostFactory.Run(x =>
            {
                x.Service<ScannerService>(
                    conf =>
                    {
                        conf.ConstructUsing(() => new ScannerService(props));
                        conf.WhenStarted(srv => srv.Start());
                        conf.WhenStopped(srv => srv.Stop());
                    }
                ).UseNLog(logFactory);

                x.SetDescription("ImgScaner Service");
                x.SetDisplayName("ImgScaner Service");
                x.SetServiceName("ImgScaner Service");
                x.RunAsLocalService();
            });
        }
    }
}
