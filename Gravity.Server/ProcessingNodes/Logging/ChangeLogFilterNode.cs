using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;

namespace Gravity.Server.ProcessingNodes.Logging
{
    internal class ChangeLogFilterNode: ProcessingNode
    {
        public string OutputNode { get; set; }
        public LogLevel MaximumLogLevel { get; set; }
        public LogType[] LogTypes { get; set; }

        private INode _nextNode;
        private string _logLevelsMessage;

        public override void UpdateStatus()
        {
            Offline = _nextNode == null || _nextNode.Offline;
        }

        public override void Bind(INodeGraph nodeGraph)
        {
            _nextNode = nodeGraph.NodeByName(OutputNode);

            _logLevelsMessage = LogTypes == null || LogTypes.Length == 0
                ? "all log types"
                : string.Join(", ", LogTypes);
        }

        public override Task ProcessRequestAsync(IRequestContext context)
        {
            if (Disabled)
            {
                context.Log?.Log(LogType.Step, LogLevel.Standard, () =>
                    $"Log level change '{Name}' is disabled, continuing with current logging level");
            }
            else
            {
                context.Log?.Log(LogType.Step, LogLevel.Standard, () =>
                    $"Changing the logging level to '{MaximumLogLevel}' and logging {_logLevelsMessage}");

                context.Log?.SetFilter(LogTypes, MaximumLogLevel);
            }

            return _nextNode.ProcessRequestAsync(context);
        }
    }
}