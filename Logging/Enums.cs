using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nistec.Logging
{
    [Flags]
    public enum LoggerLevel
    {
        None = 1,
        Debug = 2,
        Info = 4,
        Warn = 8,
        Error = 16,
        Fatal = 32,
        Trace = 64,
        All = 128
    }

    [Flags]
    public enum LoggerMode
    {
        None = 1,
        File = 2,
        Console = 4,
        Trace = 8
    }

    [Flags]
    public enum LoggerRolling
    {
        None = 0,
        Date = 1,
        Size = 2
    }
}
