using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;

namespace Gravity.Server.Interfaces
{
    internal interface ILogFactory
    {
        /// <summary>
        /// Creates a new logger and adds it to the Owin context
        /// </summary>
        ILog Create(IOwinContext context);

        /// <summary>
        /// Gets the log associated with an Owin context or null
        /// if no log was created
        /// </summary>
        ILog Get(IOwinContext context);
    }

    internal enum LogLevel
    {
        Important = 1,
        Standard = 2,
        Detailed = 3,
        VeryDetailed = 4
    }

    [Flags]
    internal enum LogType
    {
        Stats = 1,
        Step = 2,
        Logic = 4,
        Request = 8,
        Timing = 16,
        Caching = 32,
        Pooling = 64,
        TcpIp = 128,
        Exception = 256,
        Health = 512
    }

    internal interface ILog: IDisposable
    {
        void Log(LogType type, LogLevel level, Func<string> messageFunc);
    }

}