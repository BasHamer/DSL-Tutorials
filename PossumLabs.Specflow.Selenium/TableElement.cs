using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using PossumLabs.Specflow.Core;
using PossumLabs.Specflow.Selenium.Selectors;

namespace PossumLabs.Specflow.Selenium
{
    public class TableElement
    {
        public TableElement(IWebElement table, IWebDriver driver, ElementFactory elementFactory, XpathProvider xpathProvider)
        {
            RootElement = table;
            Driver = driver;
            IsValid = true;
            Header = new Dictionary<string, int>();
            ElementFactory = elementFactory;
            XpathProvider = xpathProvider;
        }

        private ElementFactory ElementFactory { get; }
        private XpathProvider XpathProvider { get; }

        private IWebElement RootElement { get; }
        private IWebDriver Driver { get; }
        public Dictionary<string, int> Header { get; }

        public int GetRowId(string key)
        {
            var xpath = $"{Prefix}/tr[td[{XpathProvider.TextMatch(key)}] or td/*[{XpathProvider.TextMatch(key)}] or td/*[@value = {key.XpathEncode()}] ]/preceding-sibling::tr";
            var count = Driver.FindElements(By.XPath(xpath)).Count() + 1;
            var rowMatch = $"td[{XpathProvider.TextMatch(key)}] or td/*[{XpathProvider.TextMatch(key)}] or td/*/*[{XpathProvider.TextMatch(key)}] or td/*[@value = {key.XpathEncode()}] ";
            
            var rows = Driver.FindElements(By.XPath(xpath));

            if (rows.None())
                throw new Exception($"Unable to find the row '{key}'");
            if (rows.Many())
                throw new Exception($"Unable to uniquely identify the row '{key}', found {rows.Count()} rows that matched it");

            xpath += "/preceding-sibling::tr";
            return count;
        }

     
        public Element GetActiveElement(int rowId, string columnId)
        {
            foreach( var xpath in XpathProvider.ActiveInCell)
            {
                var elements = Driver.FindElements(By.XPath(xpath(Prefix, rowId, Header[columnId])));
                if (elements.One())
                    return ElementFactory.Create(Driver, elements.First());
            }
            throw new Exception("no active element found in cell");
        }

        public IEnumerable<Element> GetContentElement(int rowId, string columnId)
        {
            var elements = new List<IWebElement>();

            elements.AddRange(Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]/*[{XpathProvider.ActiveElements}]")));
            elements.AddRange(Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]/div/*[{XpathProvider.ActiveElements}]")));
            elements.AddRange(Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]/div/div/*[{XpathProvider.ActiveElements}]")));
            elements.AddRange(Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]//*[{XpathProvider.ActiveElements}]")));
            
            return elements.Select(e => ElementFactory.Create(Driver, e));
        }

        public string Id { get; private set; }

        public int Ordinal { get; set; }

        public string Prefix { get; private set; }

        public string Xpath { get; set; }

        public bool IsValid { get; set; }

        public void Setup()
        {
            try
            {

                var Id = RootElement.GetAttribute("id");
                if (string.IsNullOrEmpty(Id))
                    Prefix = $"({Xpath})[{Ordinal}]";
                else
                    Prefix = $"//table[@id={Id.XpathEncode()}]";

                var bodyPrefix = Prefix;
                if (Driver.FindElements(By.XPath($"{Prefix}/tbody")).Any())
                    bodyPrefix += "/tbody";

                var headPrefix = Prefix;
                if (Driver.FindElements(By.XPath($"{Prefix}/thead")).Any())
                    headPrefix += "/thead";
                else
                    headPrefix = bodyPrefix;

                Prefix = bodyPrefix;

                var headers = Driver.FindElements(By.XPath($"{headPrefix}/tr[1]/*[self::td or self::th]"));

                var index = 0;
                foreach (var h in headers)
                {
                    index++;
                    if (string.IsNullOrWhiteSpace(h.Text))
                    {
                        var elements = Driver.FindElements(By.XPath($"{headPrefix}/tr[1]/*[self::td or self::th][{index}]/*[self::div or self::ul]/*[text() or @value]"));
                        foreach (var e in elements)
                        {
                            var text = e.Text;
                            if (string.IsNullOrWhiteSpace(text))
                                text = e.GetAttribute("value");
                            text = text ?? string.Empty;
                            if (Header.ContainsKey(text))
                                continue;
                            Header.Add(text, index);
                        }
                    }
                    else
                    {
                        if (Header.ContainsKey(h.Text))
                            continue;
                        Header.Add(h.Text, index);
                    }
                }
            }
            catch
            {
                IsValid = false;
            }
        }
    }
}
