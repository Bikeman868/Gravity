using System.Collections.Generic;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;
using Ioc.Modules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules
{
    [Package]
    internal class Package: IPackage
    {
        string IPackage.Name { get { return "Gravity URL Rewrite transform"; } }
        IList<IocRegistration> IPackage.IocRegistrations { get { return _registrations; } }

        private readonly List<IocRegistration> _registrations;

        public Package()
        {
            _registrations = new List<IocRegistration>();

            _registrations.AddRange(new[]
            {
                new IocRegistration().Init<IValueGetter, Conditions.ValueGetter>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IStringMatch, Conditions.StringMatch>(IocLifetime.MultiInstance),
                new IocRegistration().Init<INumberMatch, Conditions.NumberMatch>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IConditionList, Conditions.ConditionList>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IValueConcatenator, Conditions.ValueConcatenator>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IStaticFileMatch, Conditions.StaticFileMatch>(IocLifetime.MultiInstance),
            });

            _registrations.AddRange(new[]
            {
                new IocRegistration().Init<IAbortAction, Actions.AbortRequest>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IActionList, Actions.ActionList>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IAppendAction, Actions.Append>(IocLifetime.MultiInstance),
                new IocRegistration().Init<ICustomResponse, Actions.CustomResponse>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IDeleteAction, Actions.Delete>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IInsertAction, Actions.Insert>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IKeepAction, Actions.Keep>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IDoNothingAction, Actions.None>(IocLifetime.MultiInstance),
                new IocRegistration().Init<INormalizeAction, Actions.Normalize>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IRedirectAction, Actions.Redirect>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IReplaceAction, Actions.Replace>(IocLifetime.MultiInstance),
            //    new IocRegistration().Init<ITruncateAction, Actions.Truncate>(IocLifetime.MultiInstance),
            });

            _registrations.AddRange(new[]
            {
                new IocRegistration().Init<IRuleResult, Rules.RuleResult>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IRuleList, Rules.RuleList>(IocLifetime.MultiInstance),
                new IocRegistration().Init<IRule, Rules.Rule>(IocLifetime.MultiInstance),
            });

            //_registrations.AddRange(new[]
            //{
            //    new IocRegistration().Init<IAbsoluteUrlOperation, Operations.AbsoluteUrlOperation>(IocLifetime.MultiInstance),
            //    new IocRegistration().Init<ILowerCaseOperation, Operations.LowerCaseOperation>(IocLifetime.MultiInstance),
            //    new IocRegistration().Init<IRewriteMapOperation, Operations.RewriteMapOperation>(IocLifetime.MultiInstance),
            //    new IocRegistration().Init<IUpperCaseOperation, Operations.UpperCaseOperation>(IocLifetime.MultiInstance),
            //    new IocRegistration().Init<IUrlDecodeOperation, Operations.UrlDecodeOperation>(IocLifetime.MultiInstance),
            //    new IocRegistration().Init<IUrlEncodeOperation, Operations.UrlEncodeOperation>(IocLifetime.MultiInstance),
            //});
        }
    }
}