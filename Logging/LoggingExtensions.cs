using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nistec.Logging
{
    public static class LoggingExtensions
    {
        public static void Debug(this ILogger logger, string message)
        {
            FilteredLog(logger, LoggerLevel.Debug, message, null);
        }
        public static void Info(this ILogger logger, string message)
        {
            FilteredLog(logger, LoggerLevel.Info, message, null);
        }
        public static void Warn(this ILogger logger, string message)
        {
            FilteredLog(logger, LoggerLevel.Warn, message, null);
        }
        public static void Error(this ILogger logger, string message)
        {
            FilteredLog(logger, LoggerLevel.Error, message, null);
        }
        public static void Fatal(this ILogger logger, string message)
        {
            FilteredLog(logger, LoggerLevel.Fatal, message, null);
        }


        public static void Debug(this ILogger logger, string format, params object[] args)
        {
            FilteredLog(logger, LoggerLevel.Debug, format, args);
        }
        //public static void Debug(this ILogger logger, string format, bool consoleAsWell, params object[] args)
        //{
        //    FilteredLog(logger, LoggerLevel.Debug, format, args);
        //}
        public static void Info(this ILogger logger, string format, params object[] args)
        {
            FilteredLog(logger, LoggerLevel.Info, format, args);
        }
        public static void Warn(this ILogger logger, string format, params object[] args)
        {
            FilteredLog(logger, LoggerLevel.Warn, format, args);
        }
        public static void Error(this ILogger logger, string format, params object[] args)
        {
            FilteredLog(logger, LoggerLevel.Error, format, args);
        }
        public static void Fatal(this ILogger logger, string format, params object[] args)
        {
            FilteredLog(logger, LoggerLevel.Fatal, format, args);
        }

        public static void Trace(this ILogger logger, string method, bool begin)
        {
            if (logger.IsEnabled(LoggerLevel.Trace))
            {
                logger.Trace(method, begin);
            }
        }

        public static void Exception(this ILogger logger, string message, Exception exception)
        {
            if (logger.IsEnabled(LoggerLevel.Error))
            {
                logger.Exception(message, exception, false, false);
            }
        }

        public static void Exception(this ILogger logger, string message, Exception exception, bool innerException)
        {
            if (logger.IsEnabled(LoggerLevel.Error))
            {
                logger.Exception(message, exception, innerException, false);
            }
        }

        public static void Exception(this ILogger logger, string message, Exception exception, bool innerException, bool addStackTrace)
        {
            if (logger.IsEnabled(LoggerLevel.Error))
            {
                logger.Exception(message, exception, innerException, addStackTrace);
            }
        }

        public static void EventInfo(this ILogger logger, string appName, string format, params object[] args)
        {
            FilteredLog(logger, LoggerLevel.Info, format, args);
            EventLogger.Write(appName, StrFormat(format, args), System.Diagnostics.EventLogEntryType.Information);
        }
        public static void EventWarning(this ILogger logger, string appName, string format, params object[] args)
        {
            FilteredLog(logger, LoggerLevel.Warn, format, args);
            EventLogger.Write(appName, StrFormat(format, args), System.Diagnostics.EventLogEntryType.Warning);
        }
        public static void EventError(this ILogger logger, string appName, string format, params object[] args)
        {
            FilteredLog(logger, LoggerLevel.Error, format, args);
            EventLogger.Write(appName, StrFormat(format, args), System.Diagnostics.EventLogEntryType.Error);
        }

        private static void FilteredLog(ILogger logger, LoggerLevel level, string format, object[] objects)
        {
            if (logger.IsEnabled(level))
            {
                logger.Log(level, format, objects);
            }
        }

        internal static string StrFormat(string message, params object[] args)
        {
            if (args == null)
                return message;
            else
                return string.Format(message, args);
        }
    }
}
