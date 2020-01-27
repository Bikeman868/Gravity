namespace Gravity.Server.Configuration
{
    public enum ScriptLanguage
    {
        /// <summary>
        /// The scripts are written using the same syntax as used by the
        /// URL rewriting module. See https://github.com/Bikeman868/UrlRewrite.Net
        /// </summary>
        UrlRewriteModule,

        /// <summary>
        /// The script is a set of regular expressions that replace matching 
        /// content in the request or response body
        /// </summary>
        RegexReplace
    }
}