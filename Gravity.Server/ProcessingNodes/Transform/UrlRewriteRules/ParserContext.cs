using System;
using System.Collections.Generic;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules
{
        internal class ParserContext
        {
            public IDictionary<string, IOperation> RewriteMaps;

            public ParserContext()
            {
                RewriteMaps = new Dictionary<string, IOperation>(StringComparer.OrdinalIgnoreCase);
            }
        }
}