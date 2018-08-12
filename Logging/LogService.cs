using Nistec.Logging.Generic;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nistec.Logging
{

    public class LogConnectors
    {
        public bool LogFile;
        public bool RestApi;
    }
    internal class LogItem
    {
        public DateTime Time;
        public LoggerLevel Level;
        public string Message;

        public override string ToString()
        {
            string threadid = Thread.CurrentThread.GetHashCode().ToString();
            string threadName = Thread.CurrentThread.Name;

            //string threadDisplay = (threadName == null) ? _processid + "." + threadid : _processid + "." + threadid + ". " + threadName;
            string threadDisplay = (threadName == null) ? threadid : threadid + ". " + threadName;

            return Time.ToString("s") + " (" + threadDisplay + ") -" + Enum.Format(typeof(LoggerLevel), Level, "G") + "- " + Message;
        }
    }

    public class LogServiceSettings
    {


    }

    public enum LogServiceState { None, Started, Stoped, Paused }

    public class LogService
    {

        #region membrs
        private bool Listen;
        private bool Initilize = false;
        //private bool IsAsync = true;
        #endregion

        #region settings

        private LogServiceState _State = LogServiceState.None;
        /// <summary>
        /// Get <see cref="LogServiceState"/> State.
        /// </summary>
        public LogServiceState ServiceState { get { return _State; } }

        Logger _Logger;// = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public Logger Log { get { return _Logger; } set { if (value != null) _Logger = value; } }

        /// <summary>
        /// Get current <see cref="LogServiceState"/> settings.
        /// </summary>
        public bool IsReady { get; protected set; }



        ///// <summary>
        ///// Get current <see cref="LogServiceSettings"/> settings.
        ///// </summary>
        //public LogServiceSettings Settings { get; protected set; }
        //public LogConnectors LogConnectors { get; protected set; }

        #endregion

        #region log queue

        static readonly ConcurrentQueue<LogItem> LogQueue = new ConcurrentQueue<LogItem>();

        public void WriteLine(DateTime time,LoggerLevel level,string message)
        {
            LogQueue.Enqueue(new LogItem() { Time = time, Level = level, Message = message });

            //lock (((ICollection)LogQueue).SyncRoot)
            //{
            //    LogQueue.Enqueue(new LogItem() { Time=time, Level=level, Message=message });
            //}
        }

        /// <summary>
        /// Clear log.
        /// </summary>
        public void Clear()
        {

            LogQueue.Clear();

            //lock (((ICollection)LogQueue).SyncRoot)
            //{
            //    LogQueue.Clear();
            //}
        }

        /// <summary>
        /// Read log as string array.
        /// </summary>
        /// <returns></returns>
        public static string[] ReadLog()
        {
            //List<LogItem> copy = null;
            //lock (((ICollection)LogQueue).SyncRoot)
            //{
            //    copy = LogQueue.ToList<LogItem>();
            //}

            List<LogItem> copy = LogQueue.ToList<LogItem>();
            if (copy == null)
                return new string[] { "" };
            copy.Reverse();
            return copy.ToArray().Cast<string>().ToArray();
        }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor default
        /// </summary>
        public LogService(bool autoStart=true)
        {
            //Settings = new LogServiceSettings()
            //{

            //};
            _Logger =(Logger) Logger.Instance;
            
            if (autoStart)
            {
                Start();
            }
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="autoStart"></param>
        public LogService(Logger logger, bool autoStart)
        {
            //Settings = settings;

            _Logger = (logger != null) ? logger : (Logger)Logger.Instance;

            if (autoStart)
            {
                Start();
            }
        }
        #endregion

        #region Initilize

        private Thread _listenerThread;

        private void Init()
        {

            if (Initilize)
                return;
            IsReady = false;
            OnLoad();
            Log.WriteInternal( LoggerLevel.Info ,"LogService Initilized...\n");
            IsReady = true;
        }

        protected virtual void OnLoad()
        {

        }

        protected virtual void OnStart()
        {

        }

        protected virtual void OnStop()
        {

        }

        protected virtual void OnPause()
        {

        }


        protected virtual void OnFault(string message)
        {
            
        }
        protected virtual void OnFault(string message, Exception ex)
        {
            //Log.Exception(message, ex, true);
        }


        public void Start()
        {
            try
            {
                if (_State == LogServiceState.Paused)
                {
                    _State = LogServiceState.Started;
                    OnStart();
                    return;
                }
                if (_State == LogServiceState.Started)
                    return;

                Listen = true;
                Init();
                OnStart();
                StartInternal();
                _State = LogServiceState.Started;
            }
            catch (Exception ex)
            {
                Listen = false;
                _State = LogServiceState.None;
                OnFault("The log service on start throws the error: ", ex);
            }
        }

        void StartInternal()
        {
            try
            {
                _listenerThread = new Thread(Run);
                _listenerThread.IsBackground = true;
                _listenerThread.Start();

            }
            catch (Exception ex)
            {
                OnFault("The log service async listener on Start throws the error: ", ex);
                return;
            }
        }

        public void Stop()
        {
            Listen = false;
            _State = LogServiceState.Stoped;
            OnStop();
            Log.WriteInternal(LoggerLevel.Info, "LogService stoped...");
        }

        public void Pause()
        {
            Listen = false;
            _State = LogServiceState.Paused;
            OnPause();
            Log.WriteInternal(LoggerLevel.Info, "LogService paused...");
        }
        #endregion
       
        #region Run

        private void Run()
        {
            int interval = 100;
            int count = 0;

            while (Listen)
            {
                //TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == LogServiceState.Paused)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (IsReady == false)
                    {
                        //hasFault = true;
                        OnFault("The log service is not ready, please wait for server to be ready.");
                        Thread.Sleep(1000);
                        continue;
                    }

                    count = LogQueue.Count;
                    if (count > 1000)
                        interval = 10;
                    else if (count > 100)
                        interval = 50;
                    else 
                        interval = 100;

                    if (count > 0)
                    {
                        if (count > 100)
                            interval = 10;
                        else 
                            interval = 100;

                        //var log = LogQueue.Dequeue();
                        //_Logger.WriteLine(log.Time, log.Level, log.Message);

                        LogItem log;
                        if(LogQueue.TryDequeue(out log))
                        {
                            _Logger.WriteLine(log.Time, log.Level, log.Message);
                        }

                    }
                    else
                    {
                        interval = 100;
                    }
                    
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The log service throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }
                Thread.Sleep(interval);
            }
            _State = LogServiceState.Stoped;
        }

        #endregion
    }

   
}
