using System;
using System.Collections.Generic;
using System.Text;
using OpenQA.Selenium;

namespace PossumLabs.Specflow.Selenium.Selectors
{
    public class ActiveElementSelector : Selector
    {
        public override string Type => SelectorNames.Active;
    }
}
