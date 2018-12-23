using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Text;

namespace PossumLabs.Specflow.Selenium
{
    public interface IWebDriverWrapper
    {
        IWebDriver IWebDriver { get; }
    }
}
