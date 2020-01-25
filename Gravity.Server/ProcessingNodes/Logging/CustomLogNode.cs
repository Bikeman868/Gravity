using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Gravity.Server.Utility;

namespace Gravity.Server.ProcessingNodes.Logging
{
    internal class CustomLogNode: ProcessingNode
    {
        public string OutputNode { get; set; }
        public string[] Methods { get; set; }
        public ushort[] StatusCodes { get; set; }
        public string Directory { get; set; }
        public string FileNamePrefix { get; set; }
        public TimeSpan MaximumLogFileAge { get; set; }
        public long MaximumLogFileSize { get; set; }
        public bool Detailed { get; set; }
        public string ContentType { get; set; }

        private INode _nextNode;
        private LogFileWriter _fileWriter;
        private long _key;

        public override void UpdateStatus()
        {
            Offline = _nextNode == null || _nextNode.Offline;
        }

        public override void Bind(INodeGraph nodeGraph)
        {
            _nextNode = nodeGraph.NodeByName(OutputNode);

            // This is the only supported content type in this version
            ContentType = "text/plain";

            _fileWriter = new LogFileWriter(
                new DirectoryInfo(Directory),
                FileNamePrefix,
                MaximumLogFileAge,
                MaximumLogFileSize,
                true);
        }

        public override Task ProcessRequest(IRequestContext context)
        {
            if (Disabled)
            {
                context.Log?.Log(LogType.Step, LogLevel.Detailed, () =>
                    $"Custom log '{Name}' is disabled and will not log this request");

                return _nextNode.ProcessRequest(context);
            }

            if (Methods != null && Methods.Length > 0 && !Methods.Any(m =>string.Equals(m, context.Incoming.Method, StringComparison.OrdinalIgnoreCase)))
            {
                context.Log?.Log(LogType.Step, LogLevel.Detailed, () =>
                    $"Custom log '{Name}' is not logging this request because {context.Incoming.Method} methods are not logged");

                return _nextNode.ProcessRequest(context);
            }

            return _nextNode.ProcessRequest(context)
                .ContinueWith(t =>
                {
                    if (StatusCodes != null && StatusCodes.Length > 0 && StatusCodes.All(s => s != context.Outgoing.StatusCode))
                    {
                        context.Log?.Log(LogType.Step, LogLevel.Detailed, () =>
                            $"Custom log '{Name}' is not logging this request because {context.Outgoing.StatusCode} statuses are not logged");
                        return;
                    }

                    var entries = new List<string>
                    {
                        $"Request from {context.Incoming.SourceAddress} to {context.Incoming.Method} {context.Incoming.Scheme}://{context.Incoming.DomainName}{context.Incoming.Path}{context.Incoming.Query.ToUriComponent()} resulted in {context.Outgoing.StatusCode} {context.Outgoing.ReasonPhrase}"
                    };

                    if (Detailed)
                    {
                        entries.Add("  Received headers");

                        foreach(var incomingHeader in context.Incoming.Headers)
                            entries.Add($"    {incomingHeader.Key}: {incomingHeader.Value}");

                        entries.Add("  Response headers");

                        foreach (var outgoingHeader in context.Outgoing.Headers)
                            entries.Add($"    {outgoingHeader.Key}: {outgoingHeader.Value}");
                    }

                    _fileWriter?.WriteLog(_key, entries);
                });
        }
    }
}