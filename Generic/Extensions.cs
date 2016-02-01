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
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using System.Runtime.InteropServices;
//using Nistec.Runtime;
using System.Collections.Specialized;
using System.Globalization;

namespace Nistec.Logging.Generic
{

    internal static class StringExtension
    {

        public static string[] SplitTrim(this string s, params char[] spliter)
        {
            if (s == null || spliter == null)
            {
                throw new ArgumentNullException();
            }
            string[] array = s.Split(spliter);
            foreach (string a in array)
            {
                a.Trim();
            }
            return array;
        }

        public static string[] SplitTrim(this string s, params string[] spliter)
        {
            if (s == null || spliter == null)
            {
                throw new ArgumentNullException();
            }
            string[] array = s.Split(spliter, StringSplitOptions.None);
            foreach (string a in array)
            {
                a.Trim();
            }
            return array;
        }

        public static string[] SplitTrim(this string str, string defaultValue, char splitter = ',')
        {
            string[] list = string.IsNullOrEmpty(str) ? new string[] { defaultValue } : str.SplitTrim(splitter);
            return list;
        }

        public static string[] ReTrim(this string[] str, string defaultValue = "")
        {
            if (str == null)
            {
                throw new ArgumentNullException("ReTrim.str");
            }
            for (int i = 0; i < str.Length; i++)
            {
                str[i] = string.IsNullOrEmpty(str[i]) ? defaultValue : str[i].Trim();
            }
            return str;
        }

        public static string JoinTrim(this string[] str, string splitter = ",")
        {
            if (str == null)
            {
                throw new ArgumentNullException("JoinTrim.str");
            }

            return string.Join(splitter, ReTrim(str));
        }
    }

    internal static class EnumExtension
    {
        public static T Parse<T>(string value, T defaultValue)
        {
            try
            {
                if (value == null)
                    return defaultValue;
                if (!Enum.IsDefined(typeof(T), value))
                    return defaultValue;
                return (T) Enum.Parse(typeof(T), value, true);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static object Parse(Type type, string value, object defaultValue)
        {
            try
            {
                if (value == null)
                    return defaultValue;
                if (!Enum.IsDefined(type, value))
                    return defaultValue;
                return Enum.Parse(type, value, true);
            }
            catch
            {
                return defaultValue;
            }
        }

         /// <summary>
        ///  Converts the string representation of the name or numeric value of one or
        ///  more enumerated constants to an equivalent enumerated object. A string parameter
        ///  is not case-insensitive.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public static T Parse<T>(string value)
        {
            if (value == null)
                throw new ArgumentNullException("Enum.Parse value");
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentException("Enum not defined");
            return (T)Enum.Parse(typeof(T), value, true);
        }
        
        public static string GetDescription(Enum value)
        {
           //return Enumerations.GetEnumDescription(value);

            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        /// <summary>
        /// Get enum flags
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flags"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        /// <example>
        ///    string logmode = table.Get("LogMode");
        ///    LoggerMode modFlags = LoggerMode.None;
        ///     if (!string.IsNullOrEmpty(logmode))
        ///     {
        ///            LoggerMode[] mflags = EnumExtension.GetEnumFlags<LoggerMode>(logmode, LoggerMode.None);
        ///            foreach (LoggerMode flg in mflags)
        ///            {
        ///               modFlags = modFlags | flg;
        ///            }
        ///       }
        ///       LogMode = modFlags;
        /// </example>
        public static T[] GetEnumFlags<T>(string flags, T DefaultValue)
        {
            if (flags == null)
            {
                throw new ArgumentNullException("EnumExtension.GetEnumValues.flags");
            }
            string[] args = flags.Split('|');
            return GetEnumFlags<T>(args, DefaultValue);
        }

        public static T[] GetEnumFlags<T>(string[] args, T DefaultValue)
        {
            if (args == null)
            {
                throw new ArgumentNullException("EnumExtension.GetEnumValues.args");
            }
            List<T> res = new List<T>();
            T sum = DefaultValue;
            foreach (string s in args)
            {
                T v = EnumExtension.Parse<T>(s.Trim(), DefaultValue);
                res.Add(v);
            }
            return res.ToArray();
        }

    }

    internal static class UUID
    {

        #region UID

        public static long UniqueId()
        {
            byte[] buffer = NewUuid().ToByteArray();
            return BitConverter.ToInt64(buffer, 0);
        }

        public static long UniqueId(this Guid guid)
        {
            byte[] buffer = guid.ToByteArray();
            return BitConverter.ToInt64(buffer, 0);
        }

        public static UInt64 UId()
        {
            byte[] buffer = NewUuid().ToByteArray();
            return BitConverter.ToUInt64(buffer, 0);
        }

        public static UInt64 UId(this Guid guid)
        {
            byte[] buffer = guid.ToByteArray();
            return BitConverter.ToUInt64(buffer, 0);
        }

        public static string UxId()
        {
            byte[] buffer = NewUuid().ToByteArray();
            return BitConverter.ToUInt64(buffer, 0).ToString("x");
        }

        public static string UxId(this Guid guid)
        {
            byte[] buffer = guid.ToByteArray();
            return BitConverter.ToUInt64(buffer, 0).ToString("x");
        }

        ///// <summary>
        ///// Create new unique from base62
        ///// </summary>
        ///// <returns></returns>
        //public static string NewId()
        //{
        //    return BaseConverter.ToBase62(UniqueId());
        //}

        #endregion

        #region big int
               

        public static Guid ToGuid(ulong id)
        {
            byte[] lo = BitConverter.GetBytes(id);
            byte[] hi = BitConverter.GetBytes((ulong)0);

            byte[] bytes = new byte[16];

            Array.Copy(lo, bytes, 8);
            Array.Copy(hi, 0, bytes, 8, 8);

            return new Guid(bytes);
        }

        public static Guid ToGuid(long id)
        {
            byte[] lo = BitConverter.GetBytes(id);
            byte[] hi = BitConverter.GetBytes((long)0);

            byte[] bytes = new byte[16];

            Array.Copy(lo, bytes, 8);
            Array.Copy(hi, 0, bytes, 8, 8);
            return new Guid(bytes);
        }
        #endregion

        public static Guid NewUuid()
        {

            Guid guid;
            int result = UuidCreateSequential(out guid);
            if (result == (int)RetUuidCodes.RPC_S_OK)
                return guid;
            else
                return Guid.NewGuid();
        }

        public static string GuidSegment()
        {
            return NewUuid().ToString().Split('-')[0];
        }

        #region UUID

        [DllImport("rpcrt4.dll", SetLastError = true)]
        public static extern int UuidCreateSequential(out Guid value);

        [Flags]
        public enum RetUuidCodes : int
        {
            RPC_S_OK = 0, //The call succeeded.
            RPC_S_UUID_LOCAL_ONLY = 1824, //The UUID is guaranteed to be unique to this computer only.
            RPC_S_UUID_NO_ADDRESS = 1739 //Cannot get Ethernet or token-ring hardware address for this computer.
        }

        /// <summary>
        /// This function converts a string generated by the StringFromCLSID function back into the original class identifier.
        /// </summary>
        /// <param name="sz">String that represents the class identifier</param>
        /// <param name="clsid">On return will contain the class identifier</param>
        /// <returns>
        /// Positive or zero if class identifier was obtained successfully
        /// Negative if the call failed
        /// </returns>
        [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = true)]
        public static extern int CLSIDFromString(string sz, out Guid clsid);


        #endregion

    }

}
