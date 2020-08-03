using Nistec.Logging;
using Nistec.Logging.IO.Unsafe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Nistec.Logging.temp
{
    // Nistec.Logging.Logger
//#define TRACE
 

    public class Logger : ILogger, IDisposable
    {
        public delegate string LogItemCallback(LoggerLevel level, string text);

        private string _processid;

        private string _processname;

        internal LoggerLevel LogLevel;

        internal LoggerMode LogMode;

        internal LoggerRolling LogRolling;

        internal AsyncType AsyncType;

        internal long MaxSize;

        internal string LogFilename;

        internal string LogApp;

        internal bool AsyncService;

        internal bool AsyncInvoke;

        internal bool AsyncFile;

        internal bool AutoFlush;

        internal int BufferSize = 1024;

        internal string apiUrl;

        internal string apiMethod;

        internal bool enableApi;

        private LogStream m_ls;

        private string curFilename;

        private static readonly object syncLock = new object();

        private static readonly ILogger _instance = new Logger(autoLoad: true);

        private LogService logServive;

        private bool disposed = false;

        private Dictionary<string, DateTime> _Tracetable;

        private AsyncCallback onRequestCompleted;

        public static ILogger Instance => _instance;

        public LogService LogService
        {
            get
            {
                if (logServive == null)
                {
                    logServive = new LogService(this, autoStart: true);
                }
                return logServive;
            }
        }

        protected bool IsDisposed => disposed;

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

        public event LogMessageEventHandler LogMessage;

        internal static int GetLastModifiedFileNum(string path, string filepath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filepath);
            FileInfo fileInfo = (from f in directoryInfo.GetFiles(fileNameWithoutExtension + "*")
                                 orderby f.LastWriteTime descending
                                 select f).FirstOrDefault();
            if (fileInfo == null)
            {
                return 1;
            }
            return Types.ToInt(fileInfo.Name.Replace(fileInfo.Extension, "").Replace(fileNameWithoutExtension, ""));
        }

        private void Create(string filename)
        {
            string directoryName = Path.GetDirectoryName(filename);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            LogWriteOption writeOption = (LogRolling != LoggerRolling.Size) ? LogWriteOption.SingleFileUnboundedSize : LogWriteOption.UnlimitedSequentialFiles;
            int maxNumOfFiles = (LogRolling != LoggerRolling.Size) ? 1 : int.MaxValue;
            int currentFileNum = 1;
            if (LogRolling == LoggerRolling.Size)
            {
                currentFileNum = GetLastModifiedFileNum(directoryName, filename);
            }
            m_ls = new LogStream(filename, BufferSize, writeOption, MaxSize, maxNumOfFiles, LogRolling, currentFileNum);
            curFilename = filename;
        }

        public void Open(string filename)
        {
            if (m_ls == null)
            {
                Create(filename);
            }
            else if (curFilename == null || filename != curFilename)
            {
                Close();
                Create(filename);
            }
        }

        public void Close()
        {
            if (m_ls != null)
            {
                m_ls.Close();
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
                if (!IsEnabled(level))
                {
                    return;
                }
                if (AsyncService)
                {
                    if (args == null)
                    {
                        LogService.WriteLine(DateTime.Now, level, message);
                    }
                    else
                    {
                        LogService.WriteLine(DateTime.Now, level, string.Format(message, args));
                    }
                }
                else if (AsyncInvoke)
                {
                    LogAsyncTask(level, message, args);
                }
                else if (args == null)
                {
                    WriteLine(DateTime.Now, level, message);
                }
                else
                {
                    WriteLine(DateTime.Now, level, string.Format(message, args));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logger error WriteLog: {0}", ex.Message);
            }
        }

        internal void WriteInternal(LoggerLevel level, string message, params object[] args)
        {
            try
            {
                if (IsEnabled(level))
                {
                    if (AsyncInvoke)
                    {
                        LogAsyncTask(level, message, args);
                    }
                    else if (args == null)
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

        public Logger(bool autoLoad)
        {
            LoggerInit(autoLoad ? new NetlogSettings(autoLoad: true) : null);
        }

        public Logger(NetlogSettings settings)
        {
            LoggerInit(settings);
        }

        public Logger(string logFilename, LoggerMode logMode = LoggerMode.File, LoggerLevel logLevel = LoggerLevel.All, LoggerRolling logRolling = LoggerRolling.Date, long maxFileSize = 2147483647L, string asyncTypes = "File|Invoke")
        {
            NetlogSettings settings = new NetlogSettings(logFilename, logMode, logLevel, logRolling, maxFileSize, asyncTypes);
            LoggerInit(settings);
        }

        private void LoggerInit(NetlogSettings settings)
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
            AsyncService = AsyncType.HasFlag(AsyncType.Service);
            AsyncInvoke = AsyncType.HasFlag(AsyncType.Invoke);
            AsyncFile = AsyncType.HasFlag(AsyncType.File);
            if (MaxSize <= 0)
            {
                MaxSize = long.MaxValue;
            }
        }

        ~Logger()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Close();
                _processid = null;
                _processname = null;
                LogFilename = null;
                curFilename = null;
            }
            disposed = true;
        }

        internal static string GetMethodBase()
        {
            MethodBase method = new StackTrace().GetFrame(2).GetMethod();
            return method.DeclaringType.Name + "." + method.Name;
        }

        internal static string GetDeclaringMethod()
        {
            MethodBase method = new StackTrace().GetFrame(2).GetMethod();
            return method.DeclaringType.Namespace + "." + method.DeclaringType.Name + "." + method.Name;
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

        public void LogAsyncTask(LoggerLevel level, string message, params object[] args)
        {
            Task task = Task.Factory.StartNew(delegate
            {
                try
                {
                    if (IsEnabled(level))
                    {
                        if (args != null && args.Length != 0)
                        {
                            message = string.Format(message, args);
                        }
                        WriteLine(DateTime.Now, level, message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Logger error LogAsyncTask: {0}", ex.Message);
                }
            });
        }

        protected virtual void OnLogMessage(LogMessageEventArgs e)
        {
            if (this.LogMessage != null)
            {
                this.LogMessage(this, e);
            }
        }

        private string LogItemWorker(LoggerLevel level, string text)
        {
            try
            {
                DateTime now = DateTime.Now;
                string result = string.Format("{0}: {1}", now, level.ToString() + "-" + text);
                if (IsEnabled(level))
                {
                    WriteLine(now, level, text);
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logger error LogItemWorker: {0}", ex.Message);
                return ex.Message;
            }
        }

        public void LogAsync(LoggerLevel level, string message, params object[] args)
        {
            string text = message;
            if (args != null)
            {
                text = string.Format(message, args);
            }
            LogItemCallback logItemCallback = LogItemWorker;
            IAsyncResult asyncResult = logItemCallback.BeginInvoke(level, text, CreateCallBack(), logItemCallback);
            while (!asyncResult.IsCompleted)
            {
                asyncResult.AsyncWaitHandle.WaitOne(10, exitContext: false);
            }
            logItemCallback.EndInvoke(asyncResult);
        }

        public IAsyncResult BeginLog(LoggerLevel level, string text)
        {
            return BeginLog(level, text);
        }

        public IAsyncResult BeginLog(object state, AsyncCallback callback, LoggerLevel level, string text)
        {
            LogItemCallback logItemCallback = LogItemWorker;
            if (callback == null)
            {
                callback = CreateCallBack();
            }
            return logItemCallback.BeginInvoke(level, text, callback, logItemCallback);
        }

        public string EndLog(IAsyncResult asyncResult)
        {
            LogItemCallback logItemCallback = (LogItemCallback)asyncResult.AsyncState;
            return logItemCallback.EndInvoke(asyncResult);
        }

        private AsyncCallback CreateCallBack()
        {
            if (onRequestCompleted == null)
            {
                onRequestCompleted = OnRequestCompleted;
            }
            return onRequestCompleted;
        }

        private void OnRequestCompleted(IAsyncResult asyncResult)
        {
            if (this.LogMessage != null)
            {
                OnLogMessage(new LogMessageEventArgs(this, asyncResult));
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
            if (LogMode.HasFlag(LoggerMode.File))
            {
                string filename = GetFilename();
                if (AsyncFile)
                {
                    WriteAsync(filename, text3);
                }
                else
                {
                    Write(filename, text3);
                }
            }
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

        private void Write(string fileName, string text)
        {
            try
            {
                lock (syncLock)
                {
                    Open(fileName);
                    m_ls.WriteLine(text);
                    if (!AutoFlush)
                    {
                        m_ls.FlushWrite();
                    }
                }
            }
            catch
            {
            }
        }

        private void WriteAsync(string fileName, string text)
        {
            try
            {
                IAsyncResult asyncResult = null;
                byte[] bytes = Encoding.UTF8.GetBytes(text + "\r\n");
                lock (syncLock)
                {
                    Open(fileName);
                    asyncResult = m_ls.BeginWrite(bytes, 0, bytes.Length, null, null);
                    if (!asyncResult.CompletedSynchronously)
                    {
                        asyncResult.AsyncWaitHandle.WaitOne(1000);
                    }
                    m_ls.EndWrite(asyncResult);
                    if (!AutoFlush)
                    {
                        m_ls.FlushWrite();
                    }
                }
            }
            catch
            {
            }
        }

        private string GetFilename()
        {
            if (LogRolling == LoggerRolling.Date)
            {
                return LogFilename.Replace(".log", DateTime.Today.ToString("yyyyMMdd") + ".log");
            }
            return LogFilename;
        }
    }

}
