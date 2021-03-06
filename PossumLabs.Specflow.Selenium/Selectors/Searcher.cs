﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenQA.Selenium;

namespace PossumLabs.Specflow.Selenium.Selectors
{
    internal class Searcher
    {
        public Searcher(Func<string> messages, Func<IWebDriver, IEnumerable<SelectorPrefix>, IEnumerable<Element>> search)
        {
            Search = search;
            Messages = messages;
        }

        public Func<IWebDriver, IEnumerable<SelectorPrefix>, IEnumerable<Element>> Search { get; }
        private Func<string> Messages { get; }

        internal IEnumerable<Element> SearchIn(IWebDriver driver, IEnumerable<SelectorPrefix> pefixes)
            => Search(driver, pefixes);

        public string LogFormat()
            => Messages();
    }
}
