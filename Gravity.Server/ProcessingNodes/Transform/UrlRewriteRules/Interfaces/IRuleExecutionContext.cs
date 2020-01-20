using System;
using System.Collections.Generic;
using Gravity.Server.Pipeline;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces
{
    internal interface IRuleExecutionContext
    {
        // Contextual properties
        IRequestContext Context { get; }
        IList<Action<IRuleExecutionContext>> DeferredActions { get; }
        bool UrlIsModified { get; }

        // Information parsed from the incomming request
        string OriginalHost { get; }
        string OriginalPathAndQueryString { get; }
        string OriginalPathString { get; }
        IList<string> OriginalPath { get; }
        string OriginalParametersString { get; }
        IDictionary<string, IList<string>> OriginalParameters { get; }

        // Control over the rewriten/redirected URL
        string NewHost { get; set; }
        string NewUrlString { get; set; }
        string NewPathString { get; set; }
        string NewParametersString { get; set; }
        IList<string> NewPath { get; set; }
        IDictionary<string, IList<string>> NewParameters { get; set; }

        // Change notification
        void PathChanged();
        void ParametersChanged();
        void ExecuteDeferredActions();

        // Interaction with the request - these are here to enable unit testing
        string GetOriginalServerVariable(string name);
        string GetOriginalHeader(string name);
        IEnumerable<string> GetHeaderNames();
        string GetHeader(string name);
        void SetHeader(string name, string value);
        string GetServerVariable(string name);
        void SetServerVariable(string name, string value);
    }
}
