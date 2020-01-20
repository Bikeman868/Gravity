﻿using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    internal interface IKeepAction: IAction
    {
        IKeepAction Initialize(Scope scope, string scopeIndex);
    }
}
