using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Microsoft.Owin;
using OwinFramework.Interfaces.Utility;

namespace Gravity.Server.ProcessingNodes.Routing
{
    internal class ExpressionParser: IExpressionParser
    {
        private readonly Regex _delimitedExpressionRegex = new Regex("{(.*)}", RegexOptions.Compiled);
        private readonly Regex _pathElementExpressionRegex = new Regex("^path\\[(.*)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _headerExpressionRegex = new Regex("^header\\[(.*)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _queryExpressionRegex = new Regex("^query\\[(.*)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _pathExpressionRegex = new Regex("^path$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _nullExpressionRegex = new Regex("^null$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _methodExpressionRegex = new Regex("^method$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _ipv4ExpressionRegex = new Regex("^ipv4$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _ipv6ExpressionRegex = new Regex("^ipv6$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IHostingEnvironment _hostingEnvironment;

        public ExpressionParser(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        IExpression<T> IExpressionParser.Parse<T>(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return new NullExpression<T>();

            expression = expression.Trim();

            var match = _delimitedExpressionRegex.Match(expression);
            if (!match.Success)
                return new LiteralExpression<T>(expression, _hostingEnvironment);

            expression = match.Groups[1].Value;

            var nullMatch = _nullExpressionRegex.Match(expression);
            if (nullMatch.Success)
                return new NullExpression<T>();

            var methodMatch = _methodExpressionRegex.Match(expression);
            if (methodMatch.Success)
                return new MethodExpression<T>();

            var pathMatch = _pathExpressionRegex.Match(expression);
            if (pathMatch.Success)
                return new PathExpression<T>();

            var pathElementMatch = _pathElementExpressionRegex.Match(expression);
            if (pathElementMatch.Success)
                return new PathElementExpression<T>(int.Parse(pathElementMatch.Groups[1].Value));

            var headerMatch = _headerExpressionRegex.Match(expression);
            if (headerMatch.Success)
                return new HeaderExpression<T>(headerMatch.Groups[1].Value);

            var queryMatch = _queryExpressionRegex.Match(expression);
            if (queryMatch.Success)
                return new QueryExpression<T>(queryMatch.Groups[1].Value);

            var ipv4Match = _ipv4ExpressionRegex.Match(expression);
            if (ipv4Match.Success)
                return new Ipv4Expression<T>();

            var ipv6Match = _ipv6ExpressionRegex.Match(expression);
            if (ipv6Match.Success)
                return new Ipv6Expression<T>();

            throw new Exception("Unknown expression syntax '" + expression + "'");
        }

        private class NullExpression<T> : IExpression<T>
        {
            Type IExpression<T>.BaseType => typeof(T);

            bool IExpression<T>.IsLiteral => true;

            IExpression<TNew> IExpression<T>.Cast<TNew>()
            {
                return new NullExpression<TNew>();
            }

            T IExpression<T>.Evaluate(IRequestContext context)
            {
                return default(T);
            }
        }

        private class LiteralExpression<T> : IExpression<T>
        {
            private readonly string _expression;
            private readonly T _value;
            private readonly Type _baseType;
            private readonly IHostingEnvironment _hostingEnvironment;

            Type IExpression<T>.BaseType => _baseType;

            bool IExpression<T>.IsLiteral => true;

            public LiteralExpression(int value)
            {
                _baseType = typeof(int);

                if (typeof(int) == typeof(T))
                    _value = (T)(object)value;

                _value = (T)Convert.ChangeType(value, typeof(T));
            }

            public LiteralExpression(string expression, IHostingEnvironment hostingEnvironment)
            {
                _expression = expression;
                _hostingEnvironment = hostingEnvironment;

                if (expression.StartsWith("[") && expression.EndsWith("]"))
                {
                    // This is a comma separated list of strings
                    _baseType = typeof(string[]);
                    _value = default;
                }
                else if (expression.StartsWith("(") && expression.EndsWith(")"))
                {
                    // This is a file name to load with a list of strings
                    _baseType = typeof(string[]);
                    _value = default;
                }
                else
                {
                    // This is a single literal value
                    _baseType = typeof(string);

                    if (typeof(string) == typeof(T))
                        _value = (T)(object)expression;

                    _value = (T)Convert.ChangeType(expression, typeof(T));
                }
            }

            private LiteralExpression(
                string expression, 
                object value, 
                Type baseType,
                IHostingEnvironment hostingEnvironment)
            {
                _expression = expression;
                _value = (T)value;
                _baseType = baseType;
                _hostingEnvironment = hostingEnvironment;
            }

            IExpression<TNew> IExpression<T>.Cast<TNew>()
            {
                if (typeof(TNew) == typeof(string))
                {
                    return new LiteralExpression<TNew>(
                        _expression,
                        (TNew)(object)_value,
                        _baseType,
                        _hostingEnvironment);
                }

                if (typeof(TNew) == typeof(string[]) && _baseType == typeof(string[]))
                {
                    if (_expression.StartsWith("[") && _expression.EndsWith("]"))
                    {
                        var value = _expression
                            .Substring(1, _expression.Length - 2)
                            .Split(',')
                            .Select(s => s.Trim())
                            .ToArray();

                        return new LiteralExpression<TNew>(
                            _expression,
                            (TNew)(object)value,
                            _baseType,
                            _hostingEnvironment);
                    }

                    if (_expression.StartsWith("(") && _expression.EndsWith(")"))
                    {
                        string fileName = _expression.Substring(1, _expression.Length - 2).Trim();
                        var file = new FileInfo(_hostingEnvironment.MapPath(fileName));

                        string[] value;
                        if (file.Exists)
                        {
                            using (var reader = file.OpenText())
                            {
                                var lines = new List<string>();
                                var line = new StringBuilder();

                                var fileContent = reader.ReadToEnd();
                                for (var i = 0; i < fileContent.Length; i++)
                                {
                                    var c = fileContent[i];
                                    if (c == '\r') continue;
                                    if (c == '\n')
                                    {
                                        if (line.Length > 0)
                                        {
                                            lines.Add(line.ToString().Trim());
                                            line.Clear();
                                        }
                                        continue;
                                    }
                                    if (c == '#')
                                    {
                                        while (i + 1 < fileContent.Length && fileContent[i + 1] != '\n') i++;
                                        continue;
                                    }
                                    if (line.Length == 0 && char.IsWhiteSpace(c)) continue;
                                    if (c == '\\')
                                    {
                                        line.Append(fileContent[i + 1]);
                                        i++;
                                        continue;
                                    }
                                    line.Append(c);
                                }


                                if (line.Length > 0)
                                    lines.Add(line.ToString().Trim());

                                value = lines.ToArray();
                            }
                        }
                        else
                        {
                            value = new string[0];
                        }
                        return new LiteralExpression<TNew>(
                            _expression,
                            (TNew)(object)value,
                            _baseType,
                            _hostingEnvironment);
                    }
                }

                return new LiteralExpression<TNew>(
                    _expression,
                    (T)Convert.ChangeType(_expression, typeof(T)),
                    _baseType,
                    _hostingEnvironment);
            }

            T IExpression<T>.Evaluate(IRequestContext context)
            {
                return _value;
            }
        }

        private class HeaderExpression<T> : IExpression<T>
        {
            private readonly string _headerName;

            Type IExpression<T>.BaseType => typeof(string);

            bool IExpression<T>.IsLiteral => false;

            public HeaderExpression(string headerName)
            {
                _headerName = headerName;
            }

            IExpression<TNew> IExpression<T>.Cast<TNew>()
            {
                return new HeaderExpression<TNew>(_headerName);
            }

            T IExpression<T>.Evaluate(IRequestContext context)
            {
                if (!context.Incoming.Headers.TryGetValue(_headerName, out var header))
                    header = new[] { string.Empty };

                if (typeof (string) == typeof (T))
                    return (T)(object)header[0];

                return (T)Convert.ChangeType(header[0], typeof(T));
            }
        }

        private class MethodExpression<T> : IExpression<T>
        {
            Type IExpression<T>.BaseType => typeof(string);

            bool IExpression<T>.IsLiteral => false;

            IExpression<TNew> IExpression<T>.Cast<TNew>()
            {
                return new MethodExpression<TNew>();
            }

            T IExpression<T>.Evaluate(IRequestContext context)
            {
                var method = context.Incoming.Method;

                if (typeof (string) == typeof (T))
                    return (T)(object)method;

                return (T)Convert.ChangeType(method, typeof(T));
            }
        }

        private class PathExpression<T> : IExpression<T>
        {
            Type IExpression<T>.BaseType => typeof(string);

            bool IExpression<T>.IsLiteral => false;

            IExpression<TNew> IExpression<T>.Cast<TNew>()
            {
                return new PathExpression<TNew>();
            }

            T IExpression<T>.Evaluate(IRequestContext context)
            {
                var path = context.Incoming.Path.Value;

                if (typeof(string) == typeof(T))
                    return (T)(object)path;

                return (T)Convert.ChangeType(path, typeof(T));
            }
        }

        private class Ipv4Expression<T> : IExpression<T>
        {
            Type IExpression<T>.BaseType => typeof(IPAddress);

            bool IExpression<T>.IsLiteral => false;

            IExpression<TNew> IExpression<T>.Cast<TNew>()
            {
                return new Ipv4Expression<TNew>();
            }

            T IExpression<T>.Evaluate(IRequestContext context)
            {
                var address = context.Incoming.SourceAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    ? context.Incoming.SourceAddress
                    : (context.Incoming.SourceAddress.Equals(IPAddress.IPv6Loopback)
                        ? IPAddress.Loopback
                        : context.Incoming.SourceAddress.MapToIPv4());

                if (typeof(string) == typeof(T))
                    return (T)(object)address.ToString();

                if (typeof(IPAddress) == typeof(T))
                    return (T)(object)address;

                return (T)Convert.ChangeType(address, typeof(T));
            }
        }

        private class Ipv6Expression<T> : IExpression<T>
        {
            Type IExpression<T>.BaseType => typeof(IPAddress);

            bool IExpression<T>.IsLiteral => false;

            IExpression<TNew> IExpression<T>.Cast<TNew>()
            {
                return new Ipv6Expression<TNew>();
            }

            T IExpression<T>.Evaluate(IRequestContext context)
            {
                var address = context.Incoming.SourceAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                    ? context.Incoming.SourceAddress
                    : (context.Incoming.SourceAddress.Equals(IPAddress.Loopback)
                        ? IPAddress.IPv6Loopback
                        : context.Incoming.SourceAddress.MapToIPv6());

                if (typeof(string) == typeof(T))
                    return (T)(object)address.ToString();

                if (typeof(IPAddress) == typeof(T))
                    return (T)(object)address;

                return (T)Convert.ChangeType(address, typeof(T));
            }
        }

        private class QueryExpression<T> : IExpression<T>
        {
            private readonly string _parameterName;

            Type IExpression<T>.BaseType => typeof(string);

            bool IExpression<T>.IsLiteral => false;

            public QueryExpression(string parameterName)
            {
                _parameterName = parameterName;
            }

            IExpression<TNew> IExpression<T>.Cast<TNew>()
            {
                return new QueryExpression<TNew>(_parameterName);
            }

            T IExpression<T>.Evaluate(IRequestContext context)
            {
                var parameterValue = string.Empty;

                if (context.Incoming.Query.HasValue)
                {
                    var query = context.Incoming.Query.Value;
                    if (!string.IsNullOrEmpty(query))
                    {
                        foreach (var parameter in query.Split('&'))
                        {
                            var equalsPos = parameter.IndexOf('=');
                            if (equalsPos > 0)
                            {
                                var name = Uri.UnescapeDataString(parameter.Substring(0, equalsPos));
                                if (string.Equals(name, _parameterName, StringComparison.OrdinalIgnoreCase))
                                {
                                    parameterValue = Uri.UnescapeDataString(parameter.Substring(equalsPos + 1));
                                }
                            }
                        }
                    }
                }

                if (typeof (string) == typeof (T))
                    return (T)(object)parameterValue;

                return (T)Convert.ChangeType(parameterValue, typeof(T));
            }
        }

        private class PathElementExpression<T> : IExpression<T>
        {
            private readonly int _index;

            Type IExpression<T>.BaseType => typeof(string);

            bool IExpression<T>.IsLiteral => false;

            public PathElementExpression(int index)
            {
                _index = index;
            }

            IExpression<TNew> IExpression<T>.Cast<TNew>()
            {
                return new PathElementExpression<TNew>(_index);
            }

            T IExpression<T>.Evaluate(IRequestContext context)
            {
                var value = string.Empty;

                if (_index == 0)
                {
                    value = context.Incoming.Path.Value;
                }
                else
                {
                    const string key = "gravity.SplitPath";

                    object splitPath;
                    if (!context.Environment.TryGetValue(key, out splitPath))
                    {
                        splitPath = context.Incoming.Path.Value.Split('/');
                        context.Environment[key] = splitPath;
                    }

                    var pathElements = (string[]) splitPath;

                    if (_index > 0)
                    {
                        if (pathElements.Length > _index) 
                            value = pathElements[_index];
                    }
                    else
                    {
                        if (pathElements.Length > -_index) 
                            value = pathElements[pathElements.Length + _index];
                    }
                }

                if (typeof (string) == typeof (T))
                    return (T)(object)value;

                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
    }
}