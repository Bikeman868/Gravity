using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.DataStructures
{
    internal class ExpressionParser: IExpressionParser
    {
        private readonly Regex _delimitedExpressionRegex = new Regex("{(.*)}", RegexOptions.Compiled);
        private readonly Regex _pathExpressionRegex = new Regex("path\\[(.*)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        IExpression<T> IExpressionParser.Parse<T>(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return new DefaultExpression<T>();

            expression = expression.Trim();

            var match = _delimitedExpressionRegex.Match(expression);
            if (!match.Success)
                return new LiteralExpression<T>(expression);

            expression = match.Groups[1].Value;

            var pathMatch = _pathExpressionRegex.Match(expression);
            if (pathMatch.Success)
                return new PathExpression<T>(int.Parse(pathMatch.Groups[1].Value));

            throw new Exception("Unknown expression syntax '" + expression + "'");
        }

        private class DefaultExpression<T> : IExpression<T>
        {
            T IExpression<T>.Evaluate(IOwinContext context)
            {
                return default(T);
            }
        }

        private class LiteralExpression<T> : IExpression<T>
        {
            private readonly T _value;

            public LiteralExpression(string expression)
            {
                if (typeof (string) == typeof (T))
                    _value = (T)(object)expression;

                _value = (T)Convert.ChangeType(expression, typeof (T));
            }

            T IExpression<T>.Evaluate(IOwinContext context)
            {
                return _value;
            }
        }

        private class PathExpression<T> : IExpression<T>
        {
            private readonly int _index;

            public PathExpression(int index)
            {
                _index = index;
            }

            T IExpression<T>.Evaluate(IOwinContext context)
            {
                var value = string.Empty;

                if (_index == 0)
                {
                    value = context.Request.Path.Value;
                }
                else
                {
                    const string key = "SPLIT_PATH";

                    object splitPath;
                    if (!context.Environment.TryGetValue(key, out splitPath))
                    {
                        splitPath = context.Request.Path.Value.Split('/');
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