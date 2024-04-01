using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PluginLit.Core
{
    public static class LoggerManager
    {
        private static readonly Dictionary<string, Logger> Loggers = new Dictionary<string, Logger>();

        private static Type _loggerType = typeof(UnityLogger);

        private static LoggerLevel _loggerLevel = LoggerLevel.Debug;

        public static void SetLoggerType<T>() where T : Logger
        {
            SetLoggerType(typeof(T));
        }

        public static void SetLoggerType(Type type)
        {
            _loggerType = type;
        }

        public static void SetLoggerLevel(int level)
        {
            SetLoggerLevel((LoggerLevel) level);
        }

        public static void SetLoggerLevel(LoggerLevel level)
        {
            if (_loggerLevel == level)
                return;
            
            _loggerLevel = level;
            foreach (var logger in Loggers.Values)
            {
                logger.SetLevel(level);
            }
        }
        
        public static Logger GetLogger(string tag)
        {
            if (Loggers.TryGetValue(tag, out var val))
                return val;
            
            var logger = (Logger) Activator.CreateInstance(_loggerType);
            logger.Tag = tag;
            logger.SetLevel(_loggerLevel);
            Loggers.Add(tag, logger);
            return logger;
        }
    }
}