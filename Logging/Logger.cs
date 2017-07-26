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
       
        private string _processid;
        private string _processname;
        internal LoggerLevel LogLevel;
        internal LoggerMode LogMode;
        internal LoggerRolling LogRolling;
        internal long MaxSize;
        internal string LogFilename;
        internal string LogApp;
        internal bool IsAsync;
        internal bool AutoFlush;
        internal int BufferSize = 1024;

        internal string apiUrl;
        internal string apiMethod;
        internal bool enableApi;

        LogStream m_ls;
       string curFilename;

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

        public void Log(LoggerLevel level, string format, params object[] args)
        {
            if (IsEnabled(level))
            {
                if(args==null)
                WriteLine(level, format);
                else
                WriteLine(level, string.Format(format, args));
            }
        }

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
                IsAsync = settings.IsAsync;
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
                IsAsync = false;
                LogApp = null;
                apiUrl = null;
                apiMethod = null;
                enableApi = false;
            }

            if (MaxSize <= 0)
                MaxSize = long.MaxValue;
        }
        ~Logger()
        {
            Dispose(false);
        }

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
                WriteLine(LoggerLevel.Trace, message);
            }
        }

       
        public void Exception(string message, Exception e, bool innerException, bool addStackTrace)
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
                WriteLine(LoggerLevel.Error, sb.ToString());
            }
        }

        void WriteLine(LoggerLevel msglvl, string msg, bool consoleAswell = false)
        {
            string message;
            DateTime msgtime = DateTime.Now;
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
                if (IsAsync)
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

        void WriteApi(DateTime msgtime, LoggerLevel msglvl, string message, string threadDisplay)
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
                        while (!ar.IsCompleted)
                        {
                            //Console.Write('.');
                            Thread.Sleep(10);
                        }
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
