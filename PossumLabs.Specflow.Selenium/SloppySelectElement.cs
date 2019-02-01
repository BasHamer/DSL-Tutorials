﻿using OpenQA.Selenium;
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
                    var key = text.ToUpper();
                    var options = base.WebDriver.FindElements(
                        By.XPath($"//select[@id='{id}']/option[" +
                        $"translate(@value,'abcdefghijklmnopqrstuvwxyz','ABCDEFGHIJKLMNOPQRSTUVWXYZ') ='{key}' or " +
                        $"translate(text(),'abcdefghijklmnopqrstuvwxyz','ABCDEFGHIJKLMNOPQRSTUVWXYZ') = '{key}']"));

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

        public override List<string> Values => SelectedOptions
            .SelectMany(x=>new List<string>() { x.Text, x.GetAttribute("value") })
            .Where(s=>!string.IsNullOrWhiteSpace(s))
            .ToList();
    }
}
