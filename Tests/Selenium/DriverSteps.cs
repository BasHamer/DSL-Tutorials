using BoDi;
using LegacyTest.ValueObjects;
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

        [StepArgumentTransformation]
        public UnderSelectorPrefix TransformUnderSearcherPrefix(string Constructor)
            => SelectorFactory.CreateUnderPrefix(Constructor);

        [StepArgumentTransformation]
        public RowSelectorPrefix TransformRowSearcherPrefix(string Constructor)
            => SelectorFactory.CreateRowPrefix(Constructor);

        [AfterStep]
        public void Cleanup()
        {
            if (WebDriver != null) WebDriver.LeaveFrames();
        }

        [When(@"clicking the element '(.*)'")]
        public void WhenClickingTheElement(Selector selector)
            => Executor.Execute(()
            => WebDriver.Select(selector).Click());

        [When(@"selecting the element '(.*)'")]
        public void WhenSelectingTheElement(Selector selector)
            => Executor.Execute(()
            => WebDriver.Select(selector).Select());

        [When(@"entering '(.*)' into element '(.*)'")]
        public void WhenEnteringForTheElement(ResolvedString text, Selector selector)
            => Executor.Execute(()
            => WebDriver.Select(selector).Enter(text));

        [When(@"for row '(.*)' clicking the element '(.*)'")]
        public void WhenClickingTheElementRow(RowSelectorPrefix row, Selector selector)
            => Executor.Execute(()
            => WebDriver.ForRow(row).Select(selector).Click());

        [When(@"for row '(.*)' selecting the element '(.*)'")]
        public void WhenSelectingTheElementRow(RowSelectorPrefix row, Selector selector)
            => Executor.Execute(()
            => WebDriver.ForRow(row).Select(selector).Select());

        [When(@"for row '(.*)' entering '(.*)' into element '(.*)'")]
        public void WhenEnteringForTheElementRow(RowSelectorPrefix row, ResolvedString text, Selector selector)
            => Executor.Execute(()
            => WebDriver.ForRow(row).Select(selector).Enter(text));

        [When(@"under '(.*)' clicking the element '(.*)'")]
        public void WhenClickingTheElementUnder(UnderSelectorPrefix under, Selector selector)
            => Executor.Execute(()
            => WebDriver.Under(under).Select(selector).Click());

        [When(@"under '(.*)' selecting the element '(.*)'")]
        public void WhenSelectingTheElementUnder(UnderSelectorPrefix under, Selector selector)
            => Executor.Execute(()
            => WebDriver.Under(under).Select(selector).Select());

        [When(@"under '(.*)' entering '(.*)' into element '(.*)'")]
        public void WhenEnteringForTheElementUnder(UnderSelectorPrefix under, ResolvedString text, Selector selector)
            => Executor.Execute(()
            => WebDriver.Under(under).Select(selector).Enter(text));

        [Given(@"navigated to '(.*)'")]
        public void GivenNavigatedTo(string page)
            => Executor.Execute(()
            => WebDriver.NavigateTo(page));
    }
}
