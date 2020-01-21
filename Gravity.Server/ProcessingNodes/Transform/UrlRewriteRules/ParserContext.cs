using System;
using System.Collections.Generic;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;
using Gravity.Server.Utility;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules
{
        internal class ParserContext
        {
            public IDictionary<string, IOperation> RewriteMaps;

            public ParserContext()
            {
                RewriteMaps = new DefaultDictionary<string, IOperation>(StringComparer.OrdinalIgnoreCase);
            }
        }
}