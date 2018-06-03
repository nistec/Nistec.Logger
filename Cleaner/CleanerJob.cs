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
using System.Threading;
using System.Timers;

namespace Nistec.Logging.Cleaner
{

    public class CleanerJob
    {
        bool keepAlive = false;
        TimeSpan TimeToRun;
        int interval = 40*60*1000;
        DateTime lastRun;
        DateTime nextRun;
        System.Timers.Timer timer;

        public void Start(TimeSpan timeToRun)
        {
            if (keepAlive)
                return;

            TimeToRun = timeToRun;
            DateTime Now = DateTime.Now;
            lastRun = new DateTime(Now.Year, Now.Month, Now.Day, timeToRun.Hours, timeToRun.Minutes, timeToRun.Seconds);
            nextRun = lastRun.AddDays(1);

            keepAlive = true;
            timer = new System.Timers.Timer();
            timer.Interval = (1000 * 60 * 40); // 40 minutes...
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.AutoReset = true;
            timer.Start();

            //Thread th = new Thread(new ThreadStart(Run));
            //th.IsBackground = true;
            //th.Start();
        }
        public void Stop()
        {
            keepAlive = false;
            timer.Stop();
        }
        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now > nextRun)
            {
                TimeSpan sinceLastRun = DateTime.Now - lastRun;
                if (sinceLastRun.Hours > 23)
                {
                    LogCleaner cleaner = new LogCleaner();
                    cleaner.DoClean();
                    lastRun = DateTime.Now;
                    nextRun = nextRun.AddDays(1);
                }
            }
        }

        void Run()
        {
            while (keepAlive)
            {
                //TimeToRun     TimeOfDay       lastRun                 nextRun                 Now                     TotalHours      TotalMinutes
                //03:00:00      02:00:00        2017-11-20T03:00:00     2017-11-21T03:00:00     2017-11-21T02:00:00     23              -23*60
                //03:00:00      03:00:00        2017-11-20T03:00:00     2017-11-21T03:00:00     2017-11-21T03:00:00     24              0   
                //03:00:00      04:00:00        2017-11-21T03:00:00     2017-11-22T03:00:00     2017-11-21T04:00:00     25              0   

                try
                {
                    DateTime Now = DateTime.Now;
                    int TotalHours = (int)DateTime.Now.Subtract(lastRun).TotalHours;
                    int TotalMinutes = (int)Now.TimeOfDay.Subtract(TimeToRun).TotalMinutes;

                    if (TotalMinutes <= 60 && TotalHours >= 24 && Now >= nextRun)
                    {
                        LogCleaner cleaner = new LogCleaner();
                        cleaner.DoClean();
                        lastRun = DateTime.Now;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("CleanerJob error "+ ex.Message);
                }
                Thread.Sleep(interval);
            }
        }
    }
}
