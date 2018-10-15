using OpenQA.Selenium;
using PossumLabs.Specflow.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PossumLabs.Specflow.Selenium
{
    public class SloppySelectElement : Element
    {
        private Dictionary<string, IWebElement> Options { get; }

        public SloppySelectElement(IWebElement element, IWebDriver driver) : base(element, driver)
        {
            if (element.TagName == "select")
            {
                OldStyleSelect = new OpenQA.Selenium.Support.UI.SelectElement(element);
                AvailableOptions = OldStyleSelect.Options;
                SelectedOptions = OldStyleSelect.AllSelectedOptions;
            }
            else
            {
                var listId = element.GetAttribute("list");
                AvailableOptions = driver.FindElements(By.XPath($"//datalist[@id='{listId}']/option"));
                SelectedOptions = new List<IWebElement>();
                var value = element.GetAttribute("value");
                if (!string.IsNullOrWhiteSpace(value))
                {
                    SelectedOptions.Add(AvailableOptions.First(o => o.GetAttribute("value") == value));
                }
            }
        }

        private OpenQA.Selenium.Support.UI.SelectElement OldStyleSelect { get; }
        private IList<IWebElement> AvailableOptions { get; }
        private IList<IWebElement> SelectedOptions { get; }

        public static string ToCammelCase(string s)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(s.ToLower());
        }

        public override void Enter(string text)
        {
            if (OldStyleSelect != null)
            {

                if (text == null) return;

                //WAY faster than looking for the right item and selecting by index.
                try
                {
                    OldStyleSelect.SelectByText(text);
                    return;
                }
                catch { }

                try
                {
                    OldStyleSelect.SelectByText(text.ToUpper());
                    return;
                }
                catch { }

                try
                {
                    OldStyleSelect.SelectByText(ToCammelCase(text));
                    return;
                }
                catch { }

                try
                {
                    OldStyleSelect.SelectByText(text.ToLower());
                    return;
                }
                catch { }

                //Partial match ?
                var l = OldStyleSelect.Options.ToList();
                var realText = l.Where(x => x.Text.ToLower().Contains(text.ToLower()));
                if (realText.Count() == 1)
                {
                    try
                    {
                        OldStyleSelect.SelectByIndex(l.IndexOf(realText.First()));
                        return;
                    }
                    catch { }


                }

                throw new GherkinException($"Unable to find {text} in the selection, only found {OldStyleSelect.Options.LogFormat(x => x.Text)}");
            }
            else
            {
                var options = AvailableOptions.Where(o => string.Equals(o.GetAttribute("value"), text, ComparisonDefaults.StringComparison));
                if (options.One())
                    WebElement.SendKeys(options.First().GetAttribute("value"));
                else if (options.Many())
                    throw new GherkinException("too many matches"); //TODO: cleanup
                else
                    throw new GherkinException("no matches"); //TODO: cleanup
            }
        }

        public override List<string> Values => SelectedOptions
            .SelectMany(x=>new List<string>() { x.Text, x.GetAttribute("value") })
            .Where(s=>!string.IsNullOrWhiteSpace(s))
            .ToList();
    }
}
