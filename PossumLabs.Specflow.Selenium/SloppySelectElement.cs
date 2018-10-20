using OpenQA.Selenium;
using PossumLabs.Specflow.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PossumLabs.Specflow.Selenium
{
    public class SloppySelectElement : SelectElement
    {
        private Dictionary<string, IWebElement> Options { get; }

        public SloppySelectElement(IWebElement element, IWebDriver driver) : base(element, driver)
        {
           
        }



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

                try
                {
                    OldStyleSelect.SelectByValue(text);
                    return;
                }
                catch { }

                //Partial match ?
                var l = AvailableOptions.ToList();
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
