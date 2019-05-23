using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces
{
    public interface IRuleExecutionContext
    {
        // Contextual properties
        IOwinContext Context { get; }
        IList<Action<IRuleExecutionContext>> DeferredActions { get; }
        bool UrlIsModified { get; }

        // Information parsed from the incomming request
        string OriginalUrlString { get; }
        string OriginalPathString { get; }
        IList<string> OriginalPath { get; }
        string OriginalParametersString { get; }
        IDictionary<string, IList<string>> OriginalParameters { get; }

        // Control over the rewritten/redirected URL
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
