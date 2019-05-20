using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Gravity.Server.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules
{
    internal class Script: IRequestTransform, IResponseTransform
    {
        private readonly IFactory _factory;

        public Script(
            IFactory factory, 
            Stream stream, 
            Encoding encoding)
        {
            _factory = factory;

            XDocument document;
            try
            {
                using (var reader = new StreamReader(stream, encoding))
                    document = XDocument.Load(reader);
            }
            catch(Exception ex)
            {
                throw new Exception("Failed to load url rewrite script as XDocument", ex);
            }

            var xmlRoot = document.Root;
            if (xmlRoot == null)
                throw new Exception("No root element in url rewrite script");

            if (xmlRoot.Name != "rewrite")
                throw new Exception("The rewriter rules must be an XML document with a <rewrite> root element");

            var context = new ParserContext();

            //foreach (var element in xmlRoot.Elements())
            //{
            //    if (element.Name.LocalName.ToLower() == "rewritemaps")
            //        ParseRewriteMaps(element, context);

            //    else if (element.Name.LocalName.ToLower() == "rules")
            //        ParseRulesElement(element, context, "Root");
            //}
        }

        void IRequestTransform.Transform(IOwinContext context)
        {
            Transform(context);
        }

        IOwinContext IResponseTransform.WrapOriginalRequest(IOwinContext originalContext)
        {
            return originalContext;
        }

        void IResponseTransform.Transform(IOwinContext originalContext, IOwinContext wrappedContext)
        {
        }

        private void Transform(IOwinContext context)
        {
            if (!context.Request.Path.StartsWithSegments(new PathString("/ui")))
                context.Request.Path = new PathString("/assets/images/drawings/Layers_of_OWIN.svg");
        }
        private void ParseRewriteMaps(XElement element, ParserContext context)
        {
            //foreach (var child in element.Elements())
            //{
            //    if (child.Name.LocalName.ToLower() == "rewritemap")
            //    {
            //        var rewriteMap = _factory.Create<IRewriteMapOperation>().Initialize(child);
            //        context.RewriteMaps[rewriteMap.Name.ToLower()] = rewriteMap;
            //    }
            //}
        }

        private IRuleList ParseRulesElement(XElement element, ParserContext context, string defaultName)
        {
            var name = defaultName;
            var stopProcessing = true;

            var rules = element
                .Nodes()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .Cast<XElement>()
                .Select<XElement, IRule>(e =>
                {
                    switch (e.Name.LocalName.ToLower())
                    {
                        //case "rule":
                        //    return ParseRuleElement(e, context);
                        //case "assembly":
                        //    return ParseAssemblyElement(e, context);
                        default:
                            return null;
                    }
                })
                .Where(r => r != null)
                .ToList();

            if (element.HasAttributes)
            {
                foreach (var attribute in element.Attributes())
                {
                    switch (attribute.Name.LocalName.ToLower())
                    {
                        case "name":
                            name = attribute.Value;
                            break;
                        case "stopprocessing":
                            stopProcessing = attribute.Value.ToLower() == "true";
                            break;
                        case "enabled":
                            if (attribute.Value.ToLower() != "true")
                                return null;
                            break;
                    }
                }
            }

            return _factory.Create<IRuleList>().Initialize(name, rules, stopProcessing);
        }
    }
}