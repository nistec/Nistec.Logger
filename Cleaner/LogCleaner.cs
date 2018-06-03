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
using System.IO;
using System.Configuration;

namespace Nistec.Logging.Cleaner
{
    public class LogCleaner
    {

        public int DoClean()
        {
            //Console.WriteLine("start cleaner...");
            try
            {
                string DirectoryToClean = NetlogSettings.Settings.CleanerDirectories;
                int OldThenDays = NetlogSettings.Settings.CleanerDays;
                //int.TryParse(NetlogSettings.Settings, out OldThenDays);
                if (string.IsNullOrEmpty(DirectoryToClean))
                {
                    Console.WriteLine("Invalid direcories");
                    return 0;
                }
                string[] direcories = DirectoryToClean.Split(',', ';');

                string clean_FileEx = NetlogSettings.Settings.CleanerFileEx;
                string[] fileEx = clean_FileEx.Split(',', ';');

                return DoClean(direcories, fileEx, OldThenDays);
            }
            catch (System.Exception ex1)
            {
                System.Console.Error.WriteLine("exception: " + ex1);
                return -1;
            }
            
            //Console.ReadKey();
        }

        public int DoClean(string[] direcories, string[] fileEx, int OldThenDays)
        {
            Console.WriteLine("start cleaner...");
            int count = 0;

            if (direcories == null || direcories.Length == 0)
            {
               throw new ArgumentException("Invalid direcories");
            }

            if (fileEx == null || fileEx.Length == 0)
            {
                throw new ArgumentException("Invalid fileEx");
            }

            foreach (String dir in direcories)
            {
                if (string.IsNullOrEmpty(dir))
                    continue;

                Console.WriteLine("clean directory:" + dir);

                // note: this does not recurse directories! 
                //String[] filenames = System.IO.Directory.GetFiles(dir, "*.log");

                string[] filenames = LogDirectory.GetFiles(new string[] { dir }, fileEx);//new string[] { "*.log", "*.rar", "*.zip" });

                foreach (String filename in filenames)
                {
                    FileInfo info = new FileInfo(filename);
                    if (info.LastWriteTime < DateTime.Now.AddDays(OldThenDays * -1))
                    {
                        InvokeCleanerAsync(info);
                        count++;
                    }
                }

            }
            Console.WriteLine("cleaner finshed");

            return count;
        }


        delegate void CleanerAsyncCallBack(FileInfo info);

        void InvokeCleanerAsync(FileInfo info)
        {

            CleanerAsyncCallBack d = new CleanerAsyncCallBack(InvokeDelegate);

            IAsyncResult ar = d.BeginInvoke(info, null, null);

            d.EndInvoke(ar);

        }

        void InvokeDelegate(FileInfo info)
        {

            string filename = info.FullName;
            info.Delete();
            Console.WriteLine("Deleted {0}...", filename);
        }
    }
}
