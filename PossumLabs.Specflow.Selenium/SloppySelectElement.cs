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

                if (OldStyleSelect != null)
                {
                    var id = OldStyleSelect.WrappedElement.GetAttribute("id");
                    if (string.IsNullOrWhiteSpace(id))
                    {
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
                    }
                    var key = text.ToUpper();
                    var options = FindByExactMatch(id, key);

                    if (options.One())
                    {
                        OldStyleSelect.SelectByValue(options.First().GetAttribute("value"));
                        return;
                    }
                    options = FindByContains(id, key);

                    if (options.One())
                    {
                        OldStyleSelect.SelectByValue(options.First().GetAttribute("value"));
                        return;
                    }
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

        protected virtual System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindByContains(string id, string key)
            => base.WebDriver.FindElements(
                By.XPath($"//select[@id='{id}']/option[contains(" +
                $"translate(@value,'abcdefghijklmnopqrstuvwxyz','ABCDEFGHIJKLMNOPQRSTUVWXYZ'), '{key}') or contains(" +
                $"translate(text(),'abcdefghijklmnopqrstuvwxyz','ABCDEFGHIJKLMNOPQRSTUVWXYZ'), '{key}')]"));

        protected virtual System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindByExactMatch(string id, string key)
            => base.WebDriver.FindElements(
                By.XPath($"//select[@id='{id}']/option[" +
                $"translate(@value,'abcdefghijklmnopqrstuvwxyz','ABCDEFGHIJKLMNOPQRSTUVWXYZ') ='{key}' or " +
                $"translate(text(),'abcdefghijklmnopqrstuvwxyz','ABCDEFGHIJKLMNOPQRSTUVWXYZ') = '{key}']"));

        public override List<string> Values => SelectedOptions
            .SelectMany(x=>new List<string>() { x.Text, x.GetAttribute("value") })
            .Where(s=>!string.IsNullOrWhiteSpace(s))
            .ToList();
    }
}
