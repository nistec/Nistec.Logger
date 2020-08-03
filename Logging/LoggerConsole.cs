using Nistec.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

// Nistec.Logging.LoggerConsole
//#define TRACE

namespace Nistec.Logging
{

    /// <summary>
    /// LoggerConsole
    /// </summary>
    public class LoggerConsole : ILogger
    {
        private string _processid;

        private string _processname;

        internal LoggerLevel LogLevel;

        internal LoggerMode LogMode;

        internal LoggerRolling LogRolling;

        internal AsyncType AsyncType;

        internal long MaxSize;

        internal string LogFilename;

        internal string LogApp;

        internal bool AutoFlush;

        internal int BufferSize = 1024;

        internal string apiUrl;

        internal string apiMethod;

        internal bool enableApi;

        private Dictionary<string, DateTime> _Tracetable;

        private Dictionary<string, DateTime> Tracetable
        {
            get
            {
                if (_Tracetable == null)
                {
                    _Tracetable = new Dictionary<string, DateTime>();
                }
                return _Tracetable;
            }
        }

        public LoggerConsole(bool enableConsole = false)
        {
            LogMode = ((!enableConsole) ? LoggerMode.None : LoggerMode.Console);
            LogLevel = LoggerLevel.All;
            LogRolling = LoggerRolling.None;
            LogFilename = null;
            MaxSize = long.MaxValue;
            BufferSize = 1000;
            AutoFlush = true;
            AsyncType = AsyncType.None;
            LogApp = null;
            apiUrl = null;
            apiMethod = null;
            enableApi = false;
        }

        public LoggerConsole(NetlogSettings settings)
        {
            _processid = Process.GetCurrentProcess().Id.ToString();
            _processname = Process.GetCurrentProcess().MainModule.ModuleName;
            if (settings != null)
            {
                LogMode = settings.LogMode;
                LogLevel = settings.LogLevel;
                LogFilename = settings.LogFilename;
                LogRolling = settings.LogRolling;
                MaxSize = settings.MaxFileSize;
                BufferSize = settings.BufferSize;
                AutoFlush = settings.AutoFlush;
                AsyncType = settings.AsyncType;
                LogApp = settings.LogApp;
                apiUrl = settings.ApiUrl;
                apiMethod = settings.ApiMethod;
                enableApi = settings.EnableApi;
            }
            else
            {
                LogMode = LoggerMode.None;
                LogLevel = LoggerLevel.None;
                LogRolling = LoggerRolling.None;
                LogFilename = null;
                MaxSize = long.MaxValue;
                BufferSize = 1000;
                AutoFlush = true;
                AsyncType = AsyncType.None;
                LogApp = null;
                apiUrl = null;
                apiMethod = null;
                enableApi = false;
            }
            if (MaxSize <= 0)
            {
                MaxSize = long.MaxValue;
            }
        }

        public bool IsEnabled(LoggerLevel level)
        {
            return LogLevel == LoggerLevel.All || LogLevel.HasFlag(level);
        }

        public void Log(LoggerLevel level, string message, params object[] args)
        {
            try
            {
                if (IsEnabled(level))
                {
                    if (args == null)
                    {
                        WriteLine(DateTime.Now, level, message);
                    }
                    else
                    {
                        WriteLine(DateTime.Now, level, string.Format(message, args));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logger error WriteLog: {0}", ex.Message);
            }
        }

        public void Trace(string method, bool begin)
        {
            try
            {
                if (LogLevel.HasFlag(LoggerLevel.Trace))
                {
                    string text = null;
                    string key = $"{method}:{Thread.CurrentThread.Name}";
                    DateTime value = DateTime.Now;
                    if (begin)
                    {
                        Tracetable[key] = DateTime.Now;
                        text = method + " BEGIN";
                    }
                    else if (Tracetable.TryGetValue(key, out value))
                    {
                        text = method + " END DURATION:" + value.Subtract(DateTime.Now).TotalMilliseconds;
                        Tracetable.Remove(key);
                    }
                    else
                    {
                        text = method + " END";
                    }
                    WriteLine(value, LoggerLevel.Trace, text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logger error Trace: {0}", ex.Message);
            }
        }

        public void Exception(string message, Exception e, bool innerException, bool addStackTrace)
        {
            try
            {
                if (!LogLevel.HasFlag(LoggerLevel.Error))
                {
                    return;
                }
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(message + " " + e.Message);
                if (innerException)
                {
                    for (Exception ex = e?.InnerException; ex != null; ex = ex.InnerException)
                    {
                        stringBuilder.Append(ex.Message);
                    }
                }
                if (addStackTrace)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("StackTrace:{0}", e.StackTrace);
                }
                WriteLine(DateTime.Now, LoggerLevel.Error, stringBuilder.ToString());
            }
            catch (Exception ex2)
            {
                Console.WriteLine("Logger error LogException: {0}", ex2.Message);
            }
        }

        internal void WriteLine(DateTime msgtime, LoggerLevel msglvl, string msg, bool consoleAswell = false)
        {
            string text = Thread.CurrentThread.GetHashCode().ToString();
            string name = Thread.CurrentThread.Name;
            string text2 = (name == null) ? (_processid + "." + text) : (_processid + "." + text + ". " + name);
            string text3 = msgtime.ToString("s") + " (" + text2 + ") -" + Enum.Format(typeof(LoggerLevel), msglvl, "G") + "- " + msg;
            if (consoleAswell || LogMode.HasFlag(LoggerMode.Console))
            {
                Console.WriteLine(text3);
            }
            if (LogMode.HasFlag(LoggerMode.Trace))
            {
                System.Diagnostics.Trace.WriteLine(text3);
            }
            if (enableApi && LogMode.HasFlag(LoggerMode.Api))
            {
                WriteApi(msgtime, msglvl, msg, text2);
            }
        }

        internal void WriteApi(DateTime msgtime, LoggerLevel msglvl, string message, string threadDisplay)
        {
            string json = "{'Date':'" + msgtime.ToString("s") + "','Level':'" + Enum.Format(typeof(LoggerLevel), msglvl, "G") + "','App':'" + LogApp + "','Thread':'" + threadDisplay + "','Message':'" + HttpUtility.UrlEncode(message) + "'}";
            Task.Factory.StartNew(() => RestApiConnector.SendJson(apiUrl, apiMethod, json));
        }

        public static string PathFix(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }
    }

}
