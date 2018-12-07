using System;
using System.Collections.Generic;
using System.Text;
using OpenQA.Selenium;

namespace PossumLabs.Specflow.Selenium.Selectors
{
    public class CustomSelector : Selector
    {
        public CustomSelector(string name, By by)
        {
            Init(name, by);
        }
    }
}
