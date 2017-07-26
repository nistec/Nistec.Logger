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

namespace Nistec.Logging
{
    public class NetlogSettings
    {
        public string LogApp { get; set; }
        public string LogFilename { get; set; }
        public LoggerMode LogMode { get; set; }//0=none,1=console,2=file,3=both
        public LoggerLevel LogLevel { get; set; }
        public LoggerRolling LogRolling { get; set; }
        public long MaxFileSize { get; set; }
        public int BufferSize { get; set; }
        
        public bool IsAsync { get; set; }
        public bool AutoFlush { get; set; }
        public bool EnableApi { get; set; }
        public string ApiUrl { get; set; }
        public string ApiMethod { get; set; }


        public NetlogSettings(bool autoLoad)
        {
            AutoFlush = true;

            if (autoLoad)
            {
                LoadSettings();
            }
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
            IsAsync = false;
            ApiUrl = null;
            ApiMethod = null;
            EnableApi = false;
            AutoFlush = true;
            SetLogApp();
        }

        public void LoadSettings(string logFilename, LoggerMode logMode, LoggerLevel logLevel, LoggerRolling logRolling, long maxFileSize, int bufferSize)
        {
            LogFilename = logFilename;
            LogMode = logMode;
            LogLevel = logLevel;
            LogRolling = logRolling;
            MaxFileSize = maxFileSize;
            BufferSize = bufferSize;
            IsAsync = false;
            ApiUrl = null;
            ApiMethod = null;
            EnableApi = false;
            AutoFlush = true;
            SetLogApp();
        }

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
            IsAsync = false;
            string logFilename = table.Get("LogFilename");
            string logmode = table.Get("LogMode");
            string lvlFlags = table.Get("LogLevel");
            string logRolling = table.Get("LogRolling");
            long maxFileSize = table.Get<long>("MaxFileSize",0);
            int bufferSize = table.Get<int>("BufferSize", 1024);
            bool autoFlush = table.Get<bool>("AutoFlush", true); ;
            string apiUrl = table.Get("ApiUrl");
            string apiMethod = table.Get("ApiMethod");

            LoadSettings(logFilename, logmode, lvlFlags, logRolling, maxFileSize, bufferSize,autoFlush, apiUrl,apiMethod);
        }

        public void LoadSettings(string logFilename, string logmode, string lvlFlags, string logRolling, long maxFileSize, int bufferSize, bool autoFlush,string apiUrl, string apiMethod)
        {

            ApiUrl = apiUrl;
            ApiMethod = apiMethod;
            EnableApi = (!string.IsNullOrEmpty(apiUrl) && apiUrl.ToLower().StartsWith("http"));

            IsAsync = false;
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
