using System;
using Gravity.Server.Pipeline;

namespace Gravity.Server.Interfaces
{
    internal interface ILogFactory
    {
        /// <summary>
        /// Creates a new logger
        /// </summary>
        ILog Create(IRequestContext context);
        bool WillLog(LogType type, LogLevel level);
    }

    public enum LogLevel
    {
        Important = 1,
        Standard = 2,
        Detailed = 3,
        VeryDetailed = 4
    }

    [Flags]
    public enum LogType
    {
        /// <summary>
        /// Log messages about performance and throughput stats
        /// </summary>
        Stats = 1,

        /// <summary>
        /// Log messages about the progress from one node to the next in the node graph
        /// </summary>
        Step = 2,

        /// <summary>
        /// Log messages about conditional logic
        /// </summary>
        Logic = 4,

        /// <summary>
        /// Log messages about requests and responses
        /// </summary>
        Request = 8,

        /// <summary>
        /// Log messages about time measurements
        /// </summary>
        Timing = 16,

        /// <summary>
        /// Log messages about caching and reusing content
        /// </summary>
        Caching = 32,

        /// <summary>
        /// Log messages about pooling and reusing resources needed to handle requests
        /// </summary>
        Pooling = 64,

        /// <summary>
        /// Log messages about the sending and receiving of information over the wire
        /// </summary>
        TcpIp = 128,

        /// <summary>
        /// Log messages about exceptions that were thrown
        /// </summary>
        Exception = 256,

        /// <summary>
        /// Log messages about health checks on servers
        /// </summary>
        Health = 512,

        /// <summary>
        /// Log messages about DNS lookup
        /// </summary>
        Dns = 1024
    }

    internal interface ILog: IDisposable
    {
        bool WillLog(LogType type, LogLevel level);
        void SetFilter(LogType[] logTypes, LogLevel maximumLogLevel);
        void Log(LogType type, LogLevel level, Func<string> messageFunc);
    }

}