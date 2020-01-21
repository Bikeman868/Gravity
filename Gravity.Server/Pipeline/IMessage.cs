using Gravity.Server.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace Gravity.Server.Pipeline
{
    internal interface IMessage
    {
        /// <summary>
        /// For example
        ///    Host: mydomain.com:443
        ///    Content-Length: 6573
        ///    Content-Encoding: gzip
        /// </summary>
        IDictionary<string, string[]> Headers { get; }

        /// <summary>
        /// Add handlers to this list. They will be called just before
        /// the headers are sent and before starting to transmit the
        /// content
        /// </summary>
        IList<Action<IRequestContext>> OnSendHeaders { get; }

        /// <summary>
        /// The number of bytes in the content stream if known
        /// </summary>
        int? ContentLength { get; set; }

        /// <summary>
        /// A stream of content. Note that this can be never ending
        /// in the case of a music streaming service for example
        /// </summary>
        Stream Content { get; set; }
    }

    internal static class IMessageExtensions
    {
        public static void SendHeaders(this IMessage message, IRequestContext context)
        {
            if (message.OnSendHeaders != null)
            {
                foreach (var action in message.OnSendHeaders)
                    action(context);
            }
        }

        public static IDictionary<string,string> GetCookies(this IMessage message)
        {
            var cookies = new DefaultDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var cookieHeaders = message.Headers["Cookie"];
            if (cookieHeaders == null) return cookies;

            foreach (var cookieHeader in cookieHeaders)
            {
                foreach(var cookieString in cookieHeader.Replace(" ", "").Split(';'))
                {
                    var equalsPos = cookieString.IndexOf('=');
                    if (equalsPos > 0)
                    {
                        string name = cookieString.Substring(0, equalsPos);
                        string value = cookieString.Substring(equalsPos + 1);
                        cookies[name] = value;
                    }
                }
            }

            return cookies;
        }
    }
}