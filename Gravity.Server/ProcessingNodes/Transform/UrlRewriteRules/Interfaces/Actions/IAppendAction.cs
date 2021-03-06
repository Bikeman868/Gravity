﻿using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    internal interface IAppendAction: IAction
    {
        IAppendAction Initialize(Scope scope, string scopeIndex, IValueGetter valueGetter);
    }
}
