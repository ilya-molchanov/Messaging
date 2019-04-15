using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using Topshelf;
using System.Configuration;

namespace CentralServiceReceiver
{
    internal class Program
    {
        private static void Main()
        {
            var output = ConfigurationManager.AppSettings["output"];
            var logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            var cnString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
            var infoQueueName = ConfigurationManager.AppSettings["infoQueueName"];
            var stateQueueName = ConfigurationManager.AppSettings["stateQueueName"];

            var loggingConfiguration = new LoggingConfiguration();
            var target = new FileTarget
            {
                Name = "Def",
                FileName = logFolder,
                Layout = "${date} ${message} ${onexception:inner=${exception:format=toString}}"
            };

            loggingConfiguration.AddTarget(target);
            loggingConfiguration.AddRuleForAllLevels(target);

            var logFactory = new LogFactory(loggingConfiguration);
            var curLog = logFactory.GetLogger("Topshelf");
            Logger.Logger.SetLogger(curLog);

            HostFactory.Run(x =>
            {
                x.Service<CentralService>(
                    conf =>
                    {
                        conf.ConstructUsing(() => new CentralService(output, cnString, infoQueueName, stateQueueName));
                        conf.WhenStarted(srv => srv.Start());
                        conf.WhenStopped(srv => srv.Stop());
                    }
                ).UseNLog(logFactory);

                x.SetDescription("CentralReceiver Service");
                x.SetDisplayName("CentralReceiver Service");
                x.SetServiceName("CentralReceiver Service");
                x.RunAsLocalService();
            });
        }
    }
}
