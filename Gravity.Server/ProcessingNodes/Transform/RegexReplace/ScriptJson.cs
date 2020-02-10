using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.ProcessingNodes.Transform.RegexReplace
{
    /// <summary>
    /// In the case of regex replace scripts the scipt is in JSON
    /// format as defined by the serialization of this type
    /// </summary>
    internal class ScriptJson
    {
        /// <summary>
        /// These replacements will be applied to the incoming request
        /// that is destined for a back-end server
        /// </summary>
        public Replace[] IncomingReplacments { get; set; }

        /// <summary>
        /// These replacements will be applied to the outgoing response
        /// that was received from the back-end server andis being sent
        /// to the original caller
        /// </summary>
        public Replace[] OutgoingReplacments { get; set; }

        internal class Replace
        {
            /// <summary>
            /// All matches for this regular expression will be replaced
            /// </summary>
            [JsonProperty("matchRegex")]
            public string MatchRegex { get; set; }

            /// <summary>
            /// The replacement text. This can use regular expression syntax
            /// to refer to the match groups that were matched
            /// </summary>
            [JsonProperty("replaceRegex")]
            public string ReplaceRegex { get; set; }

            /// <summary>
            /// Because the request and response stream through the load
            /// balancer it needs to know how much of the stream to retain
            /// in memory to be able to match the regex. If you set this to
            /// a very large number then the whole response will be buffered
            /// in the load balancer resulting in significant memory consumption
            /// and significant delay in responding to the request.
            /// </summary>
            [JsonProperty("maxPatternLength")]
            public int MaximumPatternLength { get; set; }
        }
    }
}