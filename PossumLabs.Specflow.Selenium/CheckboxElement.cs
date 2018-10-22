﻿using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PossumLabs.Specflow.Selenium
{
    public class CheckboxElement : Element
    {
        public CheckboxElement(IWebElement element, IWebDriver driver) : base(element, driver)
        {

        }

        public override void Enter(string text)
        {
            if (WebElement.Selected)
            {
                if (string.Equals(text, "checked", StringComparison.InvariantCultureIgnoreCase))
                    noop();
                else
                    WebElement.Click();
            }
            else
            {
                if (string.Equals(text, "checked", StringComparison.InvariantCultureIgnoreCase))
                    WebElement.Click();
                else
                    noop();
            }
        }

        public override List<string> Values => new List<string>
        {
            WebElement.Selected.ToString(),
            WebElement.Selected?"checked":"unchecked"
        };

        //Do nothing, handy for if branches
        private void noop()
        {

        }
    }
}