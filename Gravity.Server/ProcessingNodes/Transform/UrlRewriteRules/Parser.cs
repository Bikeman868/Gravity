using System.IO;
using System.Text;
using Gravity.Server.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules
{
    internal class Parser : IScriptParser
    {
        private readonly IFactory _factory;

        public Parser(IFactory factory)
        {
            _factory = factory;
        }

        IRequestTransform IScriptParser.ParseRequestScript(Stream stream, Encoding encoding)
        {
            return new Script(_factory, stream, encoding, true);
        }

        IRequestTransform IScriptParser.ParseResponseScript(Stream stream, Encoding encoding)
        {
            return new Script(_factory, stream, encoding, false);
        }
    }
}