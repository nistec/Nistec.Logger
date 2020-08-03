//licHeaderNistec.Logger
//===============================================================================================================
// System  : Nistec.Lib - Nistec.Lib Class Library
// Author  : Nissim Trujman  (nissim@nistec.net)
// Updated : 01/07/2015
// Note    : Copyright 2007-2015, Nissim Trujman, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that is part of nistec library.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: http://nistec.net/license/nistec.cache-license.txt.  
// This notice, the author's name, and all copyright notices must remain intact in all applications, documentation,
// and source files.
//
//    Date     Who      Comments
// ==============================================================================================================
// 10/01/2006  Nissim   Created the code
//===============================================================================================================
//licHeader|
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections;
using System.Linq;
using Nistec.Logging.Generic;
using Nistec.Logging.IO;
using Nistec.Logging.IO.Unsafe;
using System.Threading.Tasks;
using System.Web;


namespace Nistec.Logging
{
    
    /// <summary>
    /// The class helps looging exceptions and traces.
    /// </summary>
    /// <example>
    /// specific Mode
    /// <netlogSettings  LogFilename="C:\\Logs" LogLevel="Debug|Info|Warn|Error|Trace" LogMode="File|Console|Trace" IsAsync="false"/>
    /// All
    /// <netlogSettings  LogFilename="C:\\Logs" LogLevel="All" LogMode="File|Console"  IsAsync="false"/>
    /// </example>
    public class Logger:ILogger, IDisposable
    {

        #region members
        private string _processid;
        private string _processname;
        internal LoggerLevel LogLevel;
        internal LoggerMode LogMode;
        internal LoggerRolling LogRolling;
        internal AsyncType AsyncType;
        internal long MaxSize;
        internal string LogFilename;
        internal string LogApp;
        //internal bool IsAsync;

        internal bool AsyncService;
        internal bool AsyncInvoke;
        internal bool AsyncFile;

        internal bool AutoFlush;
        internal int BufferSize = 1024;

        internal string apiUrl;
        internal string apiMethod;
        internal bool enableApi;

        LogStream m_ls;
        string curFilename;
        #endregion

        #region file methods

        internal static int GetLastModifiedFileNum(string path, string filepath)
       {
           var directory= new DirectoryInfo(path);
           var filename=  Path.GetFileNameWithoutExtension(filepath);
           var logfile = directory.GetFiles(filename + "*").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

           if (logfile == null)
               return 1;

           return Types.ToInt(logfile.Name.Replace(logfile.Extension, "").Replace(filename, ""));

       }
        private void Create(string filename)
        {
            string path=Path.GetDirectoryName(filename);
            // If there isn't such directory, create it.
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            //default is LogWriteOption.SingleFileUnboundedSize
            LogWriteOption writeOption = LogRolling == LoggerRolling.Size ? LogWriteOption.UnlimitedSequentialFiles : LogWriteOption.SingleFileUnboundedSize;

            int maxFiles = LogRolling == LoggerRolling.Size ? int.MaxValue : 1;
            int curFileNum = 1;
            if (LogRolling == LoggerRolling.Size)
            {
                curFileNum = GetLastModifiedFileNum(path, filename);
            }
            m_ls = new LogStream(filename, BufferSize, writeOption, MaxSize, maxFiles, LogRolling,curFileNum);
            
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
        #endregion

        private static readonly object syncLock = new object();

        private static readonly ILogger _instance = new Logger(true);

        public static ILogger Instance
        {
            get { return _instance; }
        }

        public bool IsEnabled(LoggerLevel level)
        {
            return (LogLevel.HasFlag(level));
        }
        /*
        public void Log(LoggerLevel level, string message, params object[] args)
        {
            try
            {
                if (IsEnabled(level))
                {

                    if (AsyncService)
                    {
                        if (args == null)
                            LogService.WriteLine(DateTime.Now, level, message);
                        else
                            LogService.WriteLine(DateTime.Now, level, string.Format(message, args));
                    }
                    else if (AsyncInvoke)
                    {
                        LogAsync(level, message, args);
                    }
                    else
                    {
                        if (args == null)
                            WriteLine(DateTime.Now, level, message);
                        else
                            WriteLine(DateTime.Now, level, string.Format(message, args));
                    }
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
                        LogAsync(level, message, args);
                    }
                    else
                    {
                        if (args == null)
                            WriteLine(DateTime.Now, level, message);
                        else
                            WriteLine(DateTime.Now, level, string.Format(message, args));

                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("Logger error WriteLog: {0}", ex.Message);
            }
        }
        */

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


        #region log service

        LogService logServive;

        public LogService LogService
        {
            get
            {
                if (logServive == null)
                {
                    logServive = new LogService(this, true);
                }
                return logServive;
            }
        }

        //void LogServiceStart()
        //{
        //    logServive = new LogService(true);
        //}


        #endregion

        #region ctor
        /*
    public Logger(bool autoLoad)
        : this(autoLoad ? new NetlogSettings(true) : null)
    {
    }

    public Logger(NetlogSettings settings)
    {
        _processid = Process.GetCurrentProcess().Id.ToString();
        _processname = Process.GetCurrentProcess().MainModule.ModuleName;

        if (settings != null)
        {
            //Settings.LoadSettings();
            LogMode = settings.LogMode;//0=none,11=console,12=file,13=both,5=trace,6=Exception
            LogLevel = settings.LogLevel;
            LogFilename = settings.LogFilename;
            LogRolling = settings.LogRolling;
            MaxSize = settings.MaxFileSize;
            BufferSize = settings.BufferSize;
            AutoFlush = settings.AutoFlush;
            //IsAsync = settings.IsAsync;
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
            //IsAsync = false;
            AsyncType = AsyncType.None;

            LogApp = null;
            apiUrl = null;
            apiMethod = null;
            enableApi = false;
        }

        AsyncService = (AsyncType.HasFlag(AsyncType.Service));
        AsyncInvoke = (AsyncType.HasFlag(AsyncType.Invoke));
        AsyncFile = (AsyncType.HasFlag(AsyncType.File));


        if (MaxSize <= 0)
            MaxSize = long.MaxValue;
    }
    */

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
            Dispose(false);
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Release all resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        bool disposed = false;
        /// <summary>
        /// Get indicate wether the current instance is Disposed.
        /// </summary>
        protected bool IsDisposed
        {
            get { return disposed; }
        }
        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing"></param>
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
        #endregion

        #region replection

        internal static string GetMethodBase()
        {
            System.Reflection.MethodBase methodBase = (System.Reflection.MethodBase)(new System.Diagnostics.StackTrace().GetFrame(2).GetMethod());
            return methodBase.DeclaringType.Name + "." + methodBase.Name;
        }
        internal static string GetDeclaringMethod()
        {
            System.Reflection.MethodBase methodBase = (System.Reflection.MethodBase)(new System.Diagnostics.StackTrace().GetFrame(2).GetMethod());
            return methodBase.DeclaringType.Namespace + "." + methodBase.DeclaringType.Name + "." + methodBase.Name;
        }
        #endregion

        #region Trace

        Dictionary<string, DateTime> _Tracetable;

        Dictionary<string, DateTime> Tracetable
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

        public void Trace(string method, bool begin)
        {
            try
            {
                if (LogLevel.HasFlag(LoggerLevel.Trace))
                {
                    string message = null;
                    string key = string.Format("{0}:{1}", method, Thread.CurrentThread.Name);
                    DateTime msgtime = DateTime.Now;
                    if (begin)
                    {
                        Tracetable[key] = DateTime.Now;
                        message = method + " " + "BEGIN";
                    }
                    else
                    {
                        if (Tracetable.TryGetValue(key, out msgtime))
                        {

                            TimeSpan sp = msgtime.Subtract(DateTime.Now);
                            message = method + " END DURATION:" + sp.TotalMilliseconds;
                            Tracetable.Remove(key);
                        }
                        else
                        {
                            message = method + " " + "END";
                        }
                    }
                    WriteLine(msgtime, LoggerLevel.Trace, message);

                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("Logger error Trace: {0}", ex.Message);
            }
        }
        #endregion

        #region async task

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


        #endregion

        #region write aysnc

        private AsyncCallback onRequestCompleted;
        //private ManualResetEvent resetEvent;

        /// <summary>
        /// Log Item Callback delegate
        /// </summary>
        /// <param name="level"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public delegate string LogItemCallback(LoggerLevel level, string text);
        /// <summary>
        /// Log Completed event
        /// </summary>
        public event LogMessageEventHandler LogMessage;
        /// <summary>
        /// OnLogCompleted
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnLogMessage(LogMessageEventArgs e)
        {
            if (LogMessage != null)
                LogMessage(this, e);
        }

        private string LogItemWorker(LoggerLevel level, string text)
        {
            //string msg = string.Format("{0}: {1}", DateTime.Now, text);

            try
            {
                DateTime msgTime = DateTime.Now;
                string msg = string.Format("{0}: {1}", msgTime, level.ToString() + "-" + text);

                if (IsEnabled(level))
                {
                    WriteLine(msgTime, level, text);
                }
                return msg;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logger error LogItemWorker: {0}", ex.Message);
                return ex.Message;
            }
            //Log(level, text);
        }

        /// <summary>
        /// AsyncLog
        /// </summary>
        /// <returns></returns>
        public void LogAsync(LoggerLevel level, string message, params object[] args)
        {
            string msg = message;

            if (args != null)
                msg= string.Format(message, args);

            LogItemCallback caller = new LogItemCallback(LogItemWorker);

            // Initiate the asychronous call.
            IAsyncResult result = caller.BeginInvoke(level, msg, CreateCallBack(), caller);

            while (!result.IsCompleted)
            {
                result.AsyncWaitHandle.WaitOne(10,false);
                //Thread.Sleep(10);
            }

            // Call EndInvoke to wait for the asynchronous call to complete,
            // and to retrieve the results.
            caller.EndInvoke(result);
        }

        /// <summary>
        /// Begin write to cache logger async.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public IAsyncResult BeginLog(LoggerLevel level, string text)
        {
            return BeginLog(level, text);
        }
        /// <summary>
        /// Begin write to cache logger async.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="callback"></param>
        /// <param name="level"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public IAsyncResult BeginLog(object state, AsyncCallback callback, LoggerLevel level, string text)
        {

            LogItemCallback caller = new LogItemCallback(LogItemWorker);

            if (callback == null)
            {
                callback = CreateCallBack();
            }

            // Initiate the asychronous call.  Include an AsyncCallback
            // delegate representing the callback method, and the data
            // needed to call EndInvoke.
            IAsyncResult result = caller.BeginInvoke(level, text, callback, caller);
            //this.resetEvent.Set();
            return result;
        }


        /// <summary>Completes the specified asynchronous receive operation.</summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        public string EndLog(IAsyncResult asyncResult)
        {
            // Retrieve the delegate.
            LogItemCallback caller = (LogItemCallback)asyncResult.AsyncState;

            // Call EndInvoke to retrieve the results.
            //caller.EndInvoke(asyncResult);

            string msg = (string)caller.EndInvoke(asyncResult);
            return msg;
        }

        private AsyncCallback CreateCallBack()
        {
            if (this.onRequestCompleted == null)
            {
                this.onRequestCompleted = new AsyncCallback(this.OnRequestCompleted);
            }
            return this.onRequestCompleted;
        }


        private void OnRequestCompleted(IAsyncResult asyncResult)
        {
            if (LogMessage != null)
                OnLogMessage(new LogMessageEventArgs(this, asyncResult));
        }

        #endregion

        #region writers

        public void Exception(string message, Exception e, bool innerException, bool addStackTrace)
        {
            try
            {
                if (LogLevel.HasFlag(LoggerLevel.Error))
                {

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(message + " " + e.Message);

                    if (innerException)
                    {
                        Exception innerEx = e == null ? null : e.InnerException;
                        while (innerEx != null)
                        {
                            sb.Append(innerEx.Message);
                            innerEx = innerEx.InnerException;
                        }
                    }
                    if (addStackTrace)
                    {
                        sb.AppendLine();
                        sb.AppendFormat("StackTrace:{0}", e.StackTrace);
                    }
                    WriteLine(DateTime.Now, LoggerLevel.Error, sb.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logger error LogException: {0}", ex.Message);
            }
        }
               

        internal void WriteLine(DateTime msgtime,LoggerLevel msglvl, string msg, bool consoleAswell = false)
        {
            string message;
            //DateTime msgtime = DateTime.Now;
            string threadid = Thread.CurrentThread.GetHashCode().ToString();
            string threadName = Thread.CurrentThread.Name;

            string threadDisplay = (threadName == null) ? _processid + "." + threadid : _processid + "." + threadid + ". " + threadName;

            message = msgtime.ToString("s") + " (" + threadDisplay + ") -" + Enum.Format(typeof(LoggerLevel), msglvl, "G") + "- " + msg;

            //if (threadName == null)
            //    message = msgtime.ToString("s") + "-<" + _processid + "." + threadid + "> " + Enum.Format(typeof(LoggerLevel), msglvl, "G") + " - " + msg;
            //else
            //    message = msgtime.ToString("s") + "-<" + _processid + "." + threadid + ". " + threadName + "> " + Enum.Format(typeof(LoggerLevel), msglvl, "G") + " - " + msg;

            if (LogMode.HasFlag(LoggerMode.File))
            {
                string filename = GetFilename();
                if (AsyncFile)
                    WriteAsync(filename, message);
                else
                    Write(filename, message);
            }

            if (consoleAswell || LogMode.HasFlag(LoggerMode.Console))
                Console.WriteLine(message);
            if (LogMode.HasFlag(LoggerMode.Trace))
                System.Diagnostics.Trace.WriteLine(message);
            if (enableApi && LogMode.HasFlag(LoggerMode.Api))
                WriteApi(msgtime, msglvl, msg, threadDisplay);

        }

        internal void WriteApi(DateTime msgtime, LoggerLevel msglvl, string message, string threadDisplay)
        {
            string json = @"{'Date':'" + msgtime.ToString("s") + "','Level':'" + Enum.Format(typeof(LoggerLevel), msglvl, "G") + "','App':'" + LogApp + "','Thread':'" + threadDisplay + "','Message':'" + HttpUtility.UrlEncode(message) + "'}";

            Task.Factory.StartNew(() => RestApiConnector.SendJson(apiUrl, apiMethod, json));
        }

        /// <summary>
        /// Fixes path separator, replaces / \ with platform separator char.
        /// </summary>
        /// <param name="path">Path to fix.</param>
        /// <returns></returns>
        public static string PathFix(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Writes specified text to log file.
        /// </summary>
        /// <param name="fileName">Log file name.</param>
        /// <param name="text">Log text.</param>
        void Write(string fileName, string text)
        {
            try
            {
                lock (syncLock)
                {
                    Open(fileName);
                    m_ls.WriteLine(text);
                    if (AutoFlush == false)
                        m_ls.FlushWrite();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Writes specified text to log file.
        /// </summary>
        /// <param name="fileName">Log file name.</param>
        /// <param name="text">Log text.</param>
        void WriteAsync(string fileName, string text)
        {

            try
            {

                IAsyncResult ar = null;
                byte[] data = Encoding.UTF8.GetBytes(text + "\r\n");

                lock (syncLock)
                {
                    Open(fileName);
                    // initiate an asynchronous write
                    ar = m_ls.BeginWrite(data, 0, data.Length, null, null);

                    if (!ar.CompletedSynchronously)
                    {
                        // write is proceeding in the background.
                        // wait for the operation to complete
                        ar.AsyncWaitHandle.WaitOne(1000);
                        //while (!ar.IsCompleted)
                        //{
                        //    //Console.Write('.');
                        //    Thread.Sleep(10);
                        //}
                    }
                    // harvest the result
                    m_ls.EndWrite(ar);
                    if (AutoFlush == false)
                        m_ls.FlushWrite();
                }
            }
            catch
            {
            }
        }

        #endregion

        ///// <summary>
        ///// Writes specified text to log file.
        ///// </summary>
        ///// <param name="fileName">Log file name.</param>
        ///// <param name="text">Log text.</param>
        //static void WriteLog(string fileName, string text)
        //{
        //    try
        //    {

        //        lock (syncLock)
        //        {
        //            // If there isn't such directory, create it.
        //            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
        //            {
        //                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
        //            }
        //        }

        //        lock (syncLock)
        //        {
        //            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
        //            {
        //                StreamWriter w = new StreamWriter(fs);     // create a Char writer 
        //                w.BaseStream.Seek(0, SeekOrigin.End);      // set the file pointer to the end
        //                w.Write(text + "\r\n");
        //                w.Flush();  // update underlying file
        //            }
        //        }
        //    }
        //    catch
        //    {
        //    }
        //}


        //void WriteLogAsync(string fileName, string text)
        //{
        //    lock (syncLock)
        //    {
        //        // If there isn't such directory, create it.
        //        if (!Directory.Exists(Path.GetDirectoryName(fileName)))
        //        {
        //            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
        //        }
        //    }
        //    byte[] data = Encoding.UTF8.GetBytes(text + "\r\n");
        //    FileStream fs = null;
        //    try
        //    {

        //        //fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 1, true);
        //        IAsyncResult ar = null;
        //        lock (syncLock)
        //        {
        //            // initiate an asynchronous write
        //            ar = fs.BeginWrite(data, 0, data.Length, null, null);
        //        }
        //        if (ar.CompletedSynchronously)
        //        {
        //            Console.WriteLine("Operation completed synchronously.");
        //        }
        //        else
        //        {
        //            // write is proceeding in the background.
        //            // wait for the operation to complete
        //            while (!ar.IsCompleted)
        //            {
        //                Console.Write('.');
        //                Thread.Sleep(10);
        //            }
        //            Console.WriteLine();
        //        }
        //        lock (syncLock)
        //        {
        //            // harvest the result
        //            m_ls.EndWrite(ar);
        //            fs.Flush();
        //        }
        //        Console.WriteLine("data written");
        //    }
        //    finally
        //    {
        //        lock (syncLock)
        //        {
        //            fs.Close();
        //        }
        //    }
        //}

        string GetFilename()
        {
            if (LogRolling == LoggerRolling.Date)
            {
                //string fileName = LogFilename;
                return LogFilename.Replace(".log", DateTime.Today.ToString("yyyyMMdd") + ".log");
            }
           
            return LogFilename;
        }


    }
}
