using System.IO;
using System.Text;

namespace Gravity.Server.ProcessingNodes.Transform
{
    internal interface IScriptParser
    {
        /// <summary>
        /// Parses a script that will transform the incoming stream
        /// </summary>
        IRequestTransform ParseRequestScript(Stream stream, Encoding encoding);

        /// <summary>
        /// Parses a script that will transform the outgoing stream
        /// </summary>
        IRequestTransform ParseResponseScript(Stream stream, Encoding encoding);
    }
}