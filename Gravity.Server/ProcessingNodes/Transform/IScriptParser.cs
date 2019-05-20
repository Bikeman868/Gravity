using System.IO;
using System.Text;

namespace Gravity.Server.ProcessingNodes.Transform
{
    internal interface IScriptParser
    {
        IRequestTransform ParseRequestScript(Stream stream, Encoding encoding);
        IResponseTransform ParseResponseScript(Stream stream, Encoding encoding);
    }
}