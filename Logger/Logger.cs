using System;
using NLog;

namespace Logger
{
    public class Logger
    {
        private static ILogger _logger;

        public static ILogger Current => _logger ?? throw new Exception("Logger must be set first.");

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }
    }
}