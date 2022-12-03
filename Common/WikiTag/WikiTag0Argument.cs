using OLabWebAPI.Utils;
using System.Collections.Generic;
using System.Xml.Linq;

namespace OLabWebAPI.Common
{
    public abstract class WikiTag0Argument : WikiTagModule
    {
        public WikiTag0Argument(OLabLogger logger, string htmlElementName) : base(logger, htmlElementName)
        {
            wikiTagPatterns.Add($"\\[\\[{GetWikiType()}\\]\\]");
        }

        protected override string BuildWikiTagHTMLElement()
        {
            XDocument doc = new XDocument();
            XElement xml = new XElement(
              GetHtmlElementName(),
              string.Empty
            );

            List<string> classes = new List<string> { GetHtmlElementName() };
            xml.SetAttributeValue("class", string.Join(" ", classes));
            xml.SetAttributeValue("props", "{props}");

            doc.Add(xml);

            // de-quote any attributes which are bindings
            string element = doc.ToString();
            element = element.Replace("\"{", "{");
            element = element.Replace("}\"", "}");

            return element;
        }

        public override string Translate(string source)
        {
            if (!HaveWikiTag(source))
                return source;

            while (HaveWikiTag(source))
            {
                string element = BuildWikiTagHTMLElement();
                _logger.LogDebug($"replacing {GetWiki()} <= {element}");
                source = ReplaceWikiTag(source, element);
            }

            return source;
        }

    }

}