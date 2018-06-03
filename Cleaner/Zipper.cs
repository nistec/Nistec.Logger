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
//using Ionic.Zip;
using System.IO;
using System.Configuration;
using System.IO.Compression;

namespace Nistec.Logging.Cleaner
{
    public class Zipper
    {
        const string zipEx = ".gz";//".zip";
        public int DoZip()
        {
            Console.WriteLine("start zipper...");
            int count = 0;
            try
            {
                string DirectoryToZip = ConfigurationManager.AppSettings["Directories"];
                int OldThenDays = 1;
                int.TryParse(ConfigurationManager.AppSettings["zip_OldThenDays"], out OldThenDays);
                if (string.IsNullOrEmpty(DirectoryToZip))
                {
                    Console.WriteLine("Invalid direcories");
                    return 0;
                }
                string[] direcories = DirectoryToZip.Split(',', ';');

                string zip_FileEx = ConfigurationManager.AppSettings["zip_FileEx"];
                string[] fileEx = zip_FileEx.Split(',', ';');

                foreach (String dir in direcories)
                {
                    if (string.IsNullOrEmpty(dir))
                        continue;

                    Console.WriteLine("zip directory:" + dir);

                    // note: this does not recurse directories! 
                    //String[] filenames = System.IO.Directory.GetFiles(dir, "*.log", SearchOption.AllDirectories);
                    string[] filenames = LogDirectory.GetFiles(new string[] { dir }, fileEx);//new string[] { "*.log", "*.rar", "*.zip" });

                    foreach (String filename in filenames)
                    {
                        FileInfo info = new FileInfo(filename);
                        if (info.LastWriteTime < DateTime.Now.AddDays(OldThenDays * -1))
                        {
                            InvokeZipAsync(info);
                            count++;
                        }
                    }

                }
            }
            catch (System.Exception ex1)
            {
                System.Console.Error.WriteLine("exception: " + ex1);
            }
            Console.WriteLine("zipper finshed");
            //Console.ReadKey();
            return count;
        }


        delegate void ZipAsyncCallBack(FileInfo info);

        void InvokeZipAsync(FileInfo info)
        {

            ZipAsyncCallBack d = new ZipAsyncCallBack(InvokeDelegate);

            IAsyncResult ar = d.BeginInvoke(info, null, null);

            d.EndInvoke(ar);

        }

        void InvokeDelegate(FileInfo info)
        {

            string filename = info.FullName;
            Console.WriteLine("Adding {0}...", filename);

            //using (ZipFile zip = new ZipFile())
            //{
            //    ZipEntry e = zip.AddFile(filename, "");
            //    e.Comment = "Added by Mc's CreateZip utility.";
            //    e.FileName = info.Name;
            //    zip.Save(Path.Combine(info.DirectoryName, info.Name + ".zip"));
            //}

            CompressFile(info);
            info.Delete();

            Console.WriteLine("Completed {0}...", filename);

        }

        public void CompressFile(FileInfo fileToCompress)
        {
            using (FileStream originalFileStream = fileToCompress.OpenRead())
            {
                if ((File.GetAttributes(fileToCompress.FullName) &
                   FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != zipEx)
                {
                    using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + zipEx))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                           CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);

                        }
                    }
                }
            }
        }
        public static void Compress(string directoryPath)
        {
            DirectoryInfo directorySelected = new DirectoryInfo(directoryPath);

            foreach (FileInfo fileToCompress in directorySelected.GetFiles())
            {
                using (FileStream originalFileStream = fileToCompress.OpenRead())
                {
                    if ((File.GetAttributes(fileToCompress.FullName) &
                       FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != zipEx)
                    {
                        using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + zipEx))
                        {
                            using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                               CompressionMode.Compress))
                            {
                                originalFileStream.CopyTo(compressionStream);

                            }
                        }
                        FileInfo info = new FileInfo(directoryPath + "\\" + fileToCompress.Name + zipEx);
                        Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
                        fileToCompress.Name, fileToCompress.Length.ToString(), info.Length.ToString());
                    }

                }
            }
        }

        public static void Decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
                    }
                }
            }
        }
    }
 
}

