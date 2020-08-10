//licHeader
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
using System.Linq;
using System.Text;
using Nistec.Logging.Generic;
using System.IO;
using System.Reflection;

namespace Nistec.Logging
{
    public class NetlogSettings
    {
        public const string EXECPATH = "EXECPATH";

        public static string GetExecutingLocation()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static readonly NetlogSettings Settings = new NetlogSettings(true);

        public string LogApp { get; set; }
        public string LogFilename { get; set; }
        public LoggerMode LogMode { get; set; }//0=none,1=console,2=file,3=both
        public LoggerLevel LogLevel { get; set; }
        public LoggerRolling LogRolling { get; set; }
        public long MaxFileSize { get; set; }
        public int BufferSize { get; set; }

        public AsyncType AsyncType { get; set; }

        public bool AutoFlush { get; set; }
        public bool EnableApi { get; set; }
        public string ApiUrl { get; set; }
        public string ApiMethod { get; set; }

        public string CleanerDirectories { get; set; }
        public string CleanerFileEx { get; set; }
        public int CleanerDays { get; set; }

        public static AsyncType GetAsyncTypeFlags(string asyncType, AsyncType defaultFlags = AsyncType.None)
        {
            AsyncType asyncType2 = defaultFlags;
            if (!string.IsNullOrEmpty(asyncType))
            {
                AsyncType[] enumFlags = EnumExtension.GetEnumFlags(asyncType, AsyncType.None);
                AsyncType[] array = enumFlags;
                foreach (AsyncType asyncType3 in array)
                {
                    asyncType2 |= asyncType3;
                }
            }
            return asyncType2;
        }

        public static string GetExecutingAssemblyPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static string GetDefaultPath(string logName, bool logsFolder = true)
        {
            string text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (logsFolder)
            {
                text = Path.Combine(text, "Logs");
                DirectoryInfo directoryInfo = new DirectoryInfo(text);
                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }
            }
            return Path.Combine(text, logName + ".log");
        }

        public NetlogSettings(bool autoLoad)
        {
            AutoFlush = true;

            if (autoLoad)
            {
                LoadSettings();
            }
        }

        public NetlogSettings(string logFilename, LoggerMode logMode = LoggerMode.File, LoggerLevel logLevel = LoggerLevel.All, LoggerRolling logRolling = LoggerRolling.Date, long maxFileSize = 2147483647L, string asyncTypes = "File|Invoke")
        {
            LoadSettings(logFilename, logMode, logLevel, logRolling, maxFileSize, 1000, asyncTypes);
        }

        internal void SetLogApp()
        {
            LogApp = Path.GetFileNameWithoutExtension(LogFilename);
        }
        
        public void LoadSettings(string logFilename)
        {
            LogFilename = logFilename;
            LogMode= LoggerMode.File;
            LogLevel = LoggerLevel.All;
            LogRolling = LoggerRolling.Date;
            MaxFileSize = long.MaxValue;
            BufferSize = 1000;
            //IsAsync = false;
            AsyncType =  AsyncType.None;

            ApiUrl = null;
            ApiMethod = null;
            EnableApi = false;
            AutoFlush = true;
            CleanerDirectories = null;
            CleanerFileEx = null;
            CleanerDays = 30;
            SetLogApp();
        }

        public void LoadSettings(string logFilename, LoggerMode logMode, LoggerLevel logLevel, LoggerRolling logRolling, long maxFileSize, int bufferSize, string asyncTypes = "None")
        {
            if (logFilename.StartsWith(EXECPATH))
                logFilename = logFilename.Replace(EXECPATH, GetExecutingLocation());
            LogFilename = logFilename;
            LogMode = logMode;
            LogLevel = logLevel;
            LogRolling = logRolling;
            MaxFileSize = maxFileSize;
            BufferSize = bufferSize;
            AsyncType = GetAsyncTypeFlags(asyncTypes);
            ApiUrl = null;
            ApiMethod = null;
            EnableApi = false;
            AutoFlush = true;
            CleanerDirectories = null;
            CleanerFileEx = null;
            CleanerDays = 30;
            SetLogApp();
        }

        //public void LoadSettings(string logFilename, LoggerMode logMode, LoggerLevel logLevel, LoggerRolling logRolling, long maxFileSize, int bufferSize)
        //{
        //    LogFilename = logFilename;
        //    LogMode = logMode;
        //    LogLevel = logLevel;
        //    LogRolling = logRolling;
        //    MaxFileSize = maxFileSize;
        //    BufferSize = bufferSize;
        //    //IsAsync = false;
        //    AsyncType = AsyncType.None;

        //    ApiUrl = null;
        //    ApiMethod = null;
        //    EnableApi = false;
        //    AutoFlush = true;
        //    CleanerDirectories = null;
        //    CleanerFileEx = null;
        //    CleanerDays = 30;
        //    SetLogApp();
        //}

        public void LoadSettings()
        {
            var config = NetlogConfig.GetConfig();
            if (config != null)
            {
                LoadSettings(config.NetlogItems);
            }
        }

        public void LoadSettings(NetConfigItems table)
        {
            //IsAsync = false;
            string logFilename = table.Get("LogFilename");
            string logmode = table.Get("LogMode");
            string lvlFlags = table.Get("LogLevel");
            //bool isAsync = table.Get<bool>("IsAsync", true);
            string asyncType = table.Get("AsyncType");

            string logRolling = table.Get("LogRolling");
            long maxFileSize = table.Get<long>("MaxFileSize", 0);
            int bufferSize = table.Get<int>("BufferSize", 1024);
            bool autoFlush = table.Get<bool>("AutoFlush", true);
            string apiUrl = table.Get("ApiUrl");
            string apiMethod = table.Get("ApiMethod");

            string cleaner_Directories = table.Get("cleaner_Directories");
            string cleaner_FileEx = table.Get("cleaner_FileEx");
            int cleaner_Days = table.Get<int>("cleaner_Days", 30);

            LoadSettings(logFilename, logmode, lvlFlags, asyncType, logRolling, maxFileSize, bufferSize, autoFlush, apiUrl, apiMethod, cleaner_Directories, cleaner_FileEx, cleaner_Days);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logFilename"></param>
        /// <param name="logmode"></param>
        /// <param name="lvlFlags"></param>
        /// <param name="asyncType"></param>
        /// <param name="logRolling"></param>
        /// <param name="maxFileSize"></param>
        /// <param name="bufferSize"></param>
        /// <param name="autoFlush"></param>
        /// <param name="apiUrl"></param>
        /// <param name="apiMethod"></param>
        /// <param name="cleaner_Directories"></param>
        /// <param name="cleaner_FileEx"></param>
        /// <param name="cleaner_Days"></param>
        public void LoadSettings(string logFilename, string logmode, string lvlFlags, string asyncType, string logRolling, long maxFileSize, int bufferSize, bool autoFlush,string apiUrl, string apiMethod, string cleaner_Directories,string cleaner_FileEx,int cleaner_Days)
        {

            ApiUrl = apiUrl;
            ApiMethod = apiMethod;
            EnableApi = (!string.IsNullOrEmpty(apiUrl) && apiUrl.ToLower().StartsWith("http"));

            CleanerDirectories = cleaner_Directories;
            CleanerFileEx = cleaner_FileEx;
            CleanerDays = cleaner_Days;

            AsyncType asyncFlags = AsyncType.None;
            if (!string.IsNullOrEmpty(asyncType))
            {

                AsyncType[] mflags = EnumExtension.GetEnumFlags<AsyncType>(asyncType, AsyncType.None);
                foreach (AsyncType flg in mflags)
                {
                    asyncFlags = asyncFlags | flg;
                }
            }
            AsyncType = asyncFlags;

            //IsAsync = isAsync;

            if (logFilename.StartsWith(EXECPATH))
                logFilename = logFilename.Replace(EXECPATH,GetExecutingLocation());

            LogApp = Path.GetFileNameWithoutExtension(logFilename);
            LogFilename = logFilename;
            MaxFileSize = maxFileSize;
            BufferSize = bufferSize;
            AutoFlush = autoFlush;
            SetLogApp();
            LoggerMode modFlags = LoggerMode.None;
            if (!string.IsNullOrEmpty(logmode))
            {

                LoggerMode[] mflags = EnumExtension.GetEnumFlags<LoggerMode>(logmode, LoggerMode.None);
                foreach (LoggerMode flg in mflags)
                {
                    modFlags = modFlags | flg;
                }
            }
            LogMode = modFlags;

            LoggerLevel lvFlags = LoggerLevel.None;
            if (!string.IsNullOrEmpty(lvlFlags))
            {
                LoggerLevel[] lflags = EnumExtension.GetEnumFlags<LoggerLevel>(lvlFlags, LoggerLevel.None);
                foreach (LoggerLevel flg in lflags)
                {
                    lvFlags = lvFlags | flg;
                }
            }

            LogRolling = LoggerRolling.Date;
            if (!string.IsNullOrEmpty(logRolling))
            {
                LogRolling = EnumExtension.Parse<LoggerRolling>(logRolling, LoggerRolling.Date);
            }

            if (lvFlags.HasFlag(LoggerLevel.All))
            {
                lvFlags = LoggerLevel.Info | LoggerLevel.Debug | LoggerLevel.Error | LoggerLevel.Trace | LoggerLevel.Warn;
            }

            LogLevel = lvFlags;
        }
    }
}
