﻿using System;
using System.Net;
using System.Text.RegularExpressions;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Microsoft.Owin;

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

        IExpression<T> IExpressionParser.Parse<T>(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return new NullExpression<T>();

            expression = expression.Trim();

            var match = _delimitedExpressionRegex.Match(expression);
            if (!match.Success)
                return new LiteralExpression<T>(expression);

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
            Type IExpression<T>.BaseType => typeof(string);

            T IExpression<T>.Evaluate(IRequestContext context)
            {
                return default(T);
            }
        }

        private class LiteralExpression<T> : IExpression<T>
        {
            private readonly T _value;

            Type IExpression<T>.BaseType => typeof(T);

            public LiteralExpression(string expression)
            {
                if (typeof (string) == typeof (T))
                    _value = (T)(object)expression;

                _value = (T)Convert.ChangeType(expression, typeof (T));
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

            public HeaderExpression(string headerName)
            {
                _headerName = headerName;
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

            public QueryExpression(string parameterName)
            {
                _parameterName = parameterName;
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

            public PathElementExpression(int index)
            {
                _index = index;
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