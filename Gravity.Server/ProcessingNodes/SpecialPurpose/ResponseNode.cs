using System.Threading.Tasks;
using System.Text;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;

namespace Gravity.Server.ProcessingNodes.SpecialPurpose
{
    internal class ResponseNode: ProcessingNode
    {
        public ushort StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string Content { get; set; }
        public string ContentFile { get; set; }
        public string[] HeaderNames { get; set; }
        public string[] HeaderValues { get; set; }

        void Bind(INodeGraph nodeGraph)
        {
            if (!string.IsNullOrWhiteSpace(ContentFile))
            {
                // TODO: Load the file
            }
        }

        public override void UpdateStatus()
        {
            Offline = Disabled;
        }

        public override Task ProcessRequest(IRequestContext context)
        {
            context.Log?.Log(LogType.Logic, LogLevel.Standard, () => $"Response node '{Name}' returning static {StatusCode} response");

            context.Outgoing.StatusCode = StatusCode;
            context.Outgoing.ReasonPhrase = ReasonPhrase;

            if (HeaderNames != null)
            {
                for (var i = 0; i < HeaderNames.Length; i++) 
                    context.Outgoing.Headers[HeaderNames[i]] = new [] { HeaderValues[i] };
            }

            var bytes = Encoding.UTF8.GetBytes(Content);

            context.Outgoing.Headers["Content-Length"] = new [] { bytes.Length.ToString() };
            context.Outgoing.SendHeaders(context);

            return context.Outgoing.Content.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}