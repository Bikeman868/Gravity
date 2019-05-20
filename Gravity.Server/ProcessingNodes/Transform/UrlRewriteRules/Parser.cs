using System.IO;
using System.Text;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules
{
    internal class Parser : IScriptParser
    {
        IRequestTransform IScriptParser.ParseRequestScript(Stream stream, Encoding encoding)
        {
            return new Script(stream, encoding);
        }

        IResponseTransform IScriptParser.ParseResponseScript(Stream stream, Encoding encoding)
        {
            return new Script(stream, encoding);
        }
    }
}