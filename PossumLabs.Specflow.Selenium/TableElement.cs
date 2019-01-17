﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using PossumLabs.Specflow.Core;

namespace PossumLabs.Specflow.Selenium
{
    public class TableElement
    {
        public TableElement(IWebElement table, IWebDriver driver)
        {
            RootElement = table;
            Driver = driver;
            IsValid = true;
            Header = new Dictionary<string, int>();
        }

        private IWebElement RootElement { get; }
        private IWebDriver Driver { get; }
        public Dictionary<string, int> Header { get; }

        public int GetRowId(string key)
        {
            var rowMatch = $"td[{TextMatch(key)}] or td/*[{TextMatch(key)}] or td/*/*[{TextMatch(key)}] or td/*[@value = {key.XpathEncode()}] ";
            var xpath = $"{Prefix}/tr[{rowMatch}]";
            var rows = Driver.FindElements(By.XPath(xpath));

            if (rows.None())
                throw new Exception($"Unable to find the row '{key}'");
            if (rows.Many())
                throw new Exception($"Unable to uniquely identify the row '{key}', found {rows.Count()} rows that matched it");

            xpath += "/preceding-sibling::tr";
            var count = Driver.FindElements(By.XPath(xpath)).Count()+1; //account for the 1 based indexing in xpaths here
            return count;
        }

        //HACK: copy paster
        virtual protected string TextMatch(string target)
            => $"text()[normalize-space(.)={target.XpathEncode()}]";

        //HACK: copy paster
        virtual protected string ActiveElements
           => "(not(@type='hidden') and ( self::a or self::button or self::input or self::select or self::textarea or @role='button' or @role='link' or @role='menuitem' ))";

        //HACK: copy paster
        virtual protected Element CreateElement(IWebDriver driver, IWebElement e)
        {
            if (e.TagName == "select" || (e.TagName == "input" && !string.IsNullOrEmpty(e.GetAttribute("list"))))
                return new SelectElement(e, driver);
            if (e.TagName == "input" && e.GetAttribute("type") == "radio")
            {
                var elements = driver.FindElements(By.XPath($"//input[@type='radio' and @name='{e.GetAttribute("name")}']"));
                return new RadioElement(elements, driver);
            }
            if (e.TagName == "input" && e.GetAttribute("type") == "checkbox")
            {
                return new CheckboxElement(e, driver);
            }
            return new Element(e, driver);
        }

        public Element GetActiveElement(int rowId, string columnId)
        {
            var elements = Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]/*[{ActiveElements}]"));
            if (elements.One())
                return CreateElement(Driver, elements.First());
            elements = Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]/div/*[{ActiveElements}]"));
            if (elements.One())
                return CreateElement(Driver, elements.First());
            elements = Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]/div/div/*[{ActiveElements}]"));
            if (elements.One())
                return CreateElement(Driver, elements.First());
            elements = Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]//*[{ActiveElements}]"));
            if (elements.One())
                return CreateElement(Driver, elements.First());
            throw new Exception("no active element found in cell");
        }

        public IEnumerable<Element> GetContentElement(int rowId, string columnId)
        {
            var elements = new List<IWebElement>();
            elements.AddRange(Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]} and text()]")));
            elements.AddRange(Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]/*[text() or @value]")));
            elements.AddRange(Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]/div/*[text() or @value]")));
            elements.AddRange(Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]/div/div/*[text() or @value]")));
            elements.AddRange(Driver.FindElements(By.XPath($"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]//*[{ActiveElements} and @value]")));

            return elements.Select(e => CreateElement(Driver, e));
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
