using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules
{
    internal class ResponseContext : IRuleExecutionContext
    {
        public IOwinContext Context { get; private set; }
        public bool UrlIsModified { get; private set; }

        private IList<System.Action<IRuleExecutionContext>> _deferredActions;

        public ResponseContext(IOwinContext context)
        {
            Context = context;
        }

        public IList<System.Action<IRuleExecutionContext>> DeferredActions
        {
            get
            {
                if (ReferenceEquals(_deferredActions, null))
                    _deferredActions = new List<System.Action<IRuleExecutionContext>>();
                return _deferredActions;
            }
        }

        private string _originalUrlString;

        public string OriginalUrlString
        {
            get
            {
                return _originalUrlString ?? (_originalUrlString = Context.Request.Uri.ToString());
            }
        }

        private int? _originalQueryPos;

        public int OriginalQueryPos
        {
            get
            {
                if (!_originalQueryPos.HasValue)
                {
                    _originalQueryPos = OriginalUrlString.IndexOf('?');
                }
                return _originalQueryPos.Value;
            }
        }

        private string _originalPathString;

        public string OriginalPathString
        {
            get
            {
                if (ReferenceEquals(_originalPathString, null))
                {
                    _originalPathString = OriginalQueryPos < 0
                        ? OriginalUrlString
                        : OriginalUrlString.Substring(0, OriginalQueryPos);
                }
                return _originalPathString;
            }
        }

        private IList<string> _originalPath;

        public IList<string> OriginalPath
        {
            get
            {
                if (ReferenceEquals(_originalPath, null))
                {
                    _originalPath = OriginalPathString
                        .Split('/')
                        .Where(e => !string.IsNullOrEmpty(e))
                        .ToList();
                    if (OriginalPathString.StartsWith("/"))
                        _originalPath.Insert(0, "");
                }
                return _originalPath;
            }
        }

        private IList<string> _newPath;

        public IList<string> NewPath
        {
            get
            {
                if (ReferenceEquals(_newPath, null))
                    _newPath = OriginalPath.ToList();
                return _newPath;
            }
            set 
            { 
                _newPath = value;
                _newPathString = null;
                UrlIsModified = true;
            }
        }

        private string _originalParametersString;

        public string OriginalParametersString
        {
            get
            {
                if (ReferenceEquals(_originalParametersString, null))
                {
                    _originalParametersString = Context.Request.QueryString.HasValue 
                        ? Context.Request.QueryString.ToString()
                        : string.Empty;
                }
                return _originalParametersString;
            }
        }

        private IDictionary<string, IList<string>> _originalParameters;
        private IDictionary<string, IList<string>> _newParameters;

        private void ParseParameters()
        {
            var parameters = OriginalParametersString
                .Split('&')
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            var originalParameters = new Dictionary<string, IList<string>>();
            var newParameters = new Dictionary<string, IList<string>>();

            foreach (var parameter in parameters)
            {
                string key;
                string value = null;
                var equalsPos = parameter.IndexOf('=');
                if (equalsPos < 0)
                {
                    key = parameter.ToLower();
                }
                else
                {
                    key = parameter.Substring(0, equalsPos).Trim().ToLower();
                    value = parameter.Substring(equalsPos + 1).Trim();
                }

                IList<string> values;
                if (originalParameters.TryGetValue(key, out values))
                {
                    values.Add(value);
                    newParameters[key].Add(value);
                }
                else
                {
                    originalParameters.Add(key, new List<string> { value });
                    newParameters.Add(key, new List<string> { value });
                }
            }

            _originalParameters = originalParameters;
            _newParameters = newParameters;
        }

        public IDictionary<string, IList<string>> OriginalParameters
        {
            get
            {
                if (ReferenceEquals(_originalParameters, null))
                    ParseParameters();
                return _originalParameters;
            }
        }

        public IDictionary<string, IList<string>> NewParameters
        {
            get
            {
                if (ReferenceEquals(_newParameters, null))
                    ParseParameters();
                return _newParameters;
            }
            set 
            { 
                _newParameters = value;
                _newParametersString = null;
                UrlIsModified = true;
            }
        }

        public void ExecuteDeferredActions()
        {
            if (ReferenceEquals(_deferredActions, null)) return;
            foreach (var action in _deferredActions)
                action(this);
        }

        public string NewUrlString
        {
            get
            {
                var path = NewPathString;
                var query = NewParametersString;
                if (string.IsNullOrEmpty(query))
                    return path;
                return path + "?" + query;
            }
            set 
            { 
                var queryPos = value.IndexOf('?');
                if (queryPos < 0)
                {
                    NewPathString = value;
                    NewParametersString = string.Empty;
                }
                else
                {
                    NewPathString = value.Substring(0, queryPos);
                    NewParametersString = value.Substring(queryPos + 1);
                }
            }
        }

        private string _newPathString;

        public string NewPathString
        {
            get
            {
                if (_newPathString == null)
                {
                    var sb = new StringBuilder(1024);

                    if (NewPath != null && NewPath.Count > 0)
                    {
                        var first = true;
                        foreach (var pathElement in NewPath)
                        {
                            if (first)
                                first = false;
                            else
                                sb.Append('/');
                            sb.Append(pathElement);
                        }
                    }
                    else
                    {
                        sb.Append('/');
                    }
                    _newPathString = sb.ToString();
                }
                return _newPathString;
            }
            set 
            { 
                _newPathString = value;
                _newPath = value
                    .Split('/')
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();
                if (value.StartsWith("/"))
                    _newPath.Insert(0, "");
                UrlIsModified = true;
            }
        }

        public void PathChanged()
        {
            _newPathString = null;
            UrlIsModified = true;
        }

        private string _newParametersString;

        public string NewParametersString
        {
            get
            {
                if (_newParametersString == null)
                {
                    if (NewParameters == null || NewParameters.Count == 0)
                    {
                        _newParametersString = string.Empty;
                    }
                    else
                    {
                        var sb = new StringBuilder(1024);
                        var first = true;
                        foreach (var param in NewParameters)
                        {
                            if (param.Value != null && param.Value.Count > 0)
                            {
                                foreach (var value in param.Value)
                                {
                                    if (!first) sb.Append('&');
                                    sb.Append(param.Key);
                                    sb.Append('=');
                                    sb.Append(value);
                                    first = false;
                                }
                            }
                        }
                        _newParametersString = sb.ToString();
                    }
                }
                return _newParametersString;
            }
            set 
            { 
                _newParametersString = value ?? string.Empty;
                var parameters = value
                    .Split('&')
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();
                _newParameters = new Dictionary<string, IList<string>>();
                foreach (var parameter in parameters)
                {
                    string key;
                    string parameterValue = null;
                    var equalsPos = parameter.IndexOf('=');
                    if (equalsPos < 0)
                    {
                        key = parameter.ToLower();
                    }
                    else
                    {
                        key = parameter.Substring(0, equalsPos).Trim().ToLower();
                        parameterValue = parameter.Substring(equalsPos + 1).Trim();
                    }

                    IList<string> values;
                    if (_newParameters.TryGetValue(key, out values))
                    {
                        values.Add(parameterValue);
                    }
                    else
                    {
                        _newParameters.Add(key, new List<string> { parameterValue });
                    }
                }
                UrlIsModified = true;
            }
        }

        public void ParametersChanged()
        {
            _newParametersString = null;
            UrlIsModified = true;
        }

        private IDictionary<string, string> _originalServerVraiables;
        private IDictionary<string, string> _originalHeaders;

        public string GetOriginalServerVariable(string name)
        {
            name = "server." + name;

            if (ReferenceEquals(_originalServerVraiables, null))
            {
                var value = Context.Request.Environment[name];
                return value == null ? string.Empty : value.ToString();
            }

            string stringValue;
            return _originalServerVraiables.TryGetValue(name, out stringValue) ? stringValue : string.Empty;
        }

        public string GetOriginalHeader(string name)
        {
            if (ReferenceEquals(_originalHeaders, null))
                return Context.Response.Headers[name];

            string value;
            return _originalHeaders.TryGetValue(name, out value) ? value : string.Empty;
        }

        public string GetServerVariable(string name)
        {
            name = "server." + name;

            object value;
            return Context.Request.Environment.TryGetValue(name, out value) ? value.ToString() : null;
        }

        public string GetHeader(string name)
        {
            return Context.Response.Headers[name];
        }

        public void SetServerVariable(string name, string value)
        {
            name = "server." + name;

            if (ReferenceEquals(_originalServerVraiables, null))
            {
                _originalServerVraiables = new Dictionary<string, string>();
                foreach (var serverVariable in Context.Request.Environment.Keys.Where(k => k.StartsWith("server.")))
                {
                    var environmentValue = Context.Request.Environment[serverVariable];
                    _originalServerVraiables[serverVariable] = environmentValue == null 
                        ? string.Empty 
                        : environmentValue.ToString();
                }
            }
            Context.Request.Environment[name] = value;
        }

        public void SetHeader(string name, string value)
        {
            if (ReferenceEquals(_originalHeaders, null))
            {
                _originalHeaders = new Dictionary<string, string>();
                foreach (var header in Context.Response.Headers)
                    _originalHeaders[header.Key] = Context.Request.Headers[header.Key];
            }
            Context.Response.Headers[name] = value;
        }

        public IEnumerable<string> GetHeaderNames()
        {
            return Context.Response.Headers.Keys;
        }
    }
}
