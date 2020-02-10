namespace Gravity.Server.Pipeline
{
    /// <summary>
    /// Represents a stream of HTTP data coming from a back-end server and
    /// being sent to the outside world as an HTTP reply
    /// </summary>
    public interface IOutgoingMessage: IMessage
    {
        /// <summary>
        /// For example 404 for not found
        /// </summary>
        ushort StatusCode { get; set; }

        // For example "Not found"
        string ReasonPhrase { get; set; }
    }
}