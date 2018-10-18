using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;

namespace PossumLabs.Specflow.Selenium
{
    public class TableElement
    {
        public TableElement(IWebElement table, IWebDriver driver)
        {
            RootElement = table;
            Driver = driver;

            Header = new Dictionary<string, int>();
        }

        private IWebElement RootElement { get; }
        private IWebDriver Driver { get; }
        public Dictionary<string, int> Header { get; }

        public int GetRowId(string key)
        {
            var xpath = $"{Prefix}/tr[td[{TextMatch(key)}] or td/*[{TextMatch(key)}] or td/*[@value = {key.XpathEncode()}] ]/preceding-sibling::tr";
            var count = Driver.FindElements(By.XPath(xpath)).Count() + 1;
            if (count == 1)
                throw new Exception("Found header row as the target row");
            return count;
        }

        //HACK: copy paster
        virtual protected string TextMatch(string target)
            => $"text()[normalize-space(.)={target.XpathEncode()}]";

        //HACK: copy paster
        virtual protected string ActiveElements
           => "( self::a or self::button or self::input or self::select or self::textarea or @role='button' or @role='link' or @role='menuitem' )";

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
            return new Element(e, driver);
        }

        public Element GetElement(int rowId, string columnId)
        {
            var xpath = $"{Prefix}/tr[{rowId}]/td[{Header[columnId]}]/*[{ActiveElements}]";
            return CreateElement(Driver, Driver.FindElement(By.XPath(xpath)));
        }

        public string Id { get; private set; }

        public int Ordinal { get; set; }

        public string Prefix { get; private set; }

        public string Xpath { get; set; }

        public void Setup()
        {
            var Id = RootElement.GetAttribute("id");
            if (string.IsNullOrEmpty(Id))
                Prefix = $"{Xpath}[{Ordinal}]";
            else
                Prefix = $"//table[@id={Id.XpathEncode()}]";

            if (Driver.FindElements(By.XPath($"{Prefix}/tbody")).Any())
                Prefix += "/tbody";

            var headers = Driver.FindElements(By.XPath($"{Prefix}/tr[1]/*[self::td or self::th]"));

            var index = 1;
            foreach(var h in headers)
            {
                if (string.IsNullOrWhiteSpace(h.Text))
                {
                    var elements = Driver.FindElements(By.XPath($"{Prefix}/tr[1]//*[text() or @value]"));
                    foreach (var e in elements)
                    {
                        var text = e.Text;
                        if (string.IsNullOrWhiteSpace(text))
                            text = e.GetAttribute("value");
                        Header.Add(text , index);
                    }
                }
                else
                    Header.Add(h.Text, index);
                index++;
            }            
        }
    }
}
