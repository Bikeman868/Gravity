using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using System;
using System.Threading.Tasks;

namespace Gravity.Server.ProcessingNodes
{
    internal abstract class ProcessingNode: INode
    {
        private bool _disabled;
        public bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                if (value) Offline = true;
            }
        }

        public string Name { get; set; }
        public bool Offline { get; protected set; }

        protected ProcessingNode()
        {
            Offline = true;
        }

        public virtual void Bind(INodeGraph nodeGraph) { }
        public virtual void Dispose() { }

        public abstract Task ProcessRequest(IRequestContext context);
        public abstract void UpdateStatus();
    }
}