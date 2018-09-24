﻿using BoDi;
using PossumLabs.Specflow.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Shim.Selenium
{
    [Binding]
    public class DriverSteps: WebDriverStepBase
    {
        public DriverSteps(IObjectContainer objectContainer) : base(objectContainer)
        { }

        [StepArgumentTransformation]
        public Selector TransformSelector(string Constructor)
            => SelectorFactory.Create(Constructor);

        [When(@"clicking the element '(.*)'")]
        public void WhenClickingTheElement(Selector selector)
            => WebDriver.Select(selector).Click();

        [When(@"selecting the element '(.*)'")]
        public void WhenSelectingTheElement(Selector selector)
            => WebDriver.Select(selector).Select();

        [When(@"entering '(.*)' into element '(.*)'")]
        public void WhenEnteringForTheElement(string text, Selector selector)
            => WebDriver.Select(selector).Enter(text);

        [Given(@"navigated to '(.*)'")]
        public void GivenNavigatedTo(string page)
            => WebDriver.NavigateTo(page);
    }
}