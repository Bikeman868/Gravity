namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions
{
    public interface IConditionList : ICondition
    {
        IConditionList Initialize(CombinationLogic logic, bool trackAllCaptures = false);

        IConditionList Add(ICondition condition);
    }
}
