using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public class Logger
    {
        public static ILog _logger = null;
        public static bool isInit = false;
        public static void Init(string configPath)
        {
            if (!isInit)
            {
                log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(configPath));
                _logger = LogManager.GetLogger("LOG");
                isInit = true;
            }
        }
        public static void Info(object message)
        {
            _logger.Info(message);
        }
        public static void Info(object message, Exception exception)
        {
            _logger.Info(message, exception);
        }
        public static void Error(object message)
        {
            _logger.Error(message);
        }
        public static void Error(object message, Exception exception)
        {
            _logger.Error(message, exception);
        }
        public static void Fatal(object message)
        {
            _logger.Fatal(message);
        }
        public static void Fatal(object message, Exception exception)
        {
            _logger.Fatal(message, exception);
        }
        public static void Debug(object message)
        {
            _logger.Debug(message);
        }
        public static void Debug(object message, Exception exception)
        {
            _logger.Debug(message, exception);
        }
        public static void Warn(object message)
        {
            _logger.Warn(message);
        }
        public static void Warn(object message, Exception exception)
        {
            _logger.Warn(message, exception);
        }

    }
}
