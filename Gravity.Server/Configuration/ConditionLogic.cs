namespace Gravity.Server.Configuration
{
    public enum ConditionLogic
    {
        /// <summary>
        /// All conditions must be true
        /// </summary>
        All,

        /// <summary>
        /// At least 1 condition must be true
        /// </summary>
        Any,

        /// <summary>
        /// All conditions must be false
        /// </summary>
        None,

        /// <summary>
        /// At least one condition must be false
        /// </summary>
        NotAll
    }
}