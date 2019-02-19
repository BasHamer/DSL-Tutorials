using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using PossumLabs.Specflow.Core.Validations;
using System.Drawing;
using PossumLabs.Specflow.Core;
using OpenQA.Selenium.Interactions;
using System.Text.RegularExpressions;

namespace PossumLabs.Specflow.Selenium
{
    public class Element
    {
        public IWebElement WebElement { get; }
        protected IWebDriver WebDriver { get; }

        public Element(IWebElement element, IWebDriver driver)
        {
            WebElement = element;
            WebDriver = driver;
        }

        public string Tag => WebElement.TagName;
        public Point Location => WebElement.Location;
        public IEnumerable<string> Classes => WebElement.GetAttribute("class").Split(' ').Select(x => x.Trim());
        public string Id => WebElement.GetAttribute("id");
        public virtual List<string> Values => new List<string>() { WebElement.GetAttribute("value"), WebElement.Text };
        public virtual string Value => WebElement.GetAttribute("value") ?? WebElement.Text;

        public string GetAttribute(string name) => WebElement.GetAttribute(name);

        public bool Displayed => WebElement.Displayed;

        public System.Drawing.Size Size { get => WebElement.Size; }
        public bool IsEnabled { get => WebElement.Enabled; }

        public void Select()
            => WebElement.Click();

        //https://www.grazitti.com/resources/articles/automating-different-input-fields-using-selenium-webdriver/

        public virtual void Enter(string text)
        {
            try
            {
                WebElement.Clear();
            }
            catch { }
            if (text == null)
                return;

            //TODO: v2 Check Boxes
            WebElement.SendKeys(text);
            if (Equivalent(WebElement.GetAttribute("value"), text))
                return;

            try
            {
                WebElement.Clear();
            }
            catch { }
            WebElement.Click(); //Works with date time elements require formatting
            WebElement.SendKeys(text);
            if (Equivalent(WebElement.GetAttribute("value"), text))
                throw new Exception($"failed setting element, desired '{text}' got '{WebElement.GetAttribute("value")}'");
        }

        protected bool Equivalent(string a, string b)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            return rgx.Replace(a, "").ToUpper() == rgx.Replace(b,"").ToUpper();
        }

        public void Click()
            => WebElement.Click();

        public string GetCssValue(string prop)
            => WebElement.GetCssValue(prop);

        public void DoubleClick()
        {
            var builder = new Actions(WebDriver);
            var action = builder.DoubleClick(WebElement);
            action.Build().Perform();
        }

        public virtual void Clear()
        => WebElement.Clear();

    }
}
