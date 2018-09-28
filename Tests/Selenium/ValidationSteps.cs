using BoDi;
using FluentAssertions;
using PossumLabs.Specflow.Core.Validations;
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
    public class ValidationSteps : WebDriverStepBase
    {
        public ValidationSteps(IObjectContainer objectContainer) : base(objectContainer)
        { }

        [StepArgumentTransformation]
        public WebValidation TransformWebValidation(string Constructor) 
            => WebValidationFactory.Create(Constructor);

        [StepArgumentTransformation]
        public TableValidation TransformForHas(Table table) 
            => WebValidationFactory.Create(table.Rows.Select(r=>table.Header.ToDictionary(h=>h, h=> WebValidationFactory.Create(r[h]))).ToList());

        [Then(@"the element '(.*)' has the value '(.*)'")]
        public void ThenTheElementHasTheValue(Selector selector, WebValidation validation)
            => WebDriver.Select(selector).Validate(validation);

        [Then(@"under '(.*)' the element '(.*)' has the value '(.*)'")]
        public void ThenUnderTheElementHasTheValue(UnderSelectorPrefix prefix, Selector selector, WebValidation validation)
            => WebDriver.Under(prefix).Select(selector).Validate(validation);

        [Then(@"for row '(.*)' the element '(.*)' has the value '(.*)'")]
        public void ThenForRowTheElementHasTheValue(RowSelectorPrefix prefix, Selector selector, WebValidation validation)
            => WebDriver.ForRow(prefix).Select(selector).Validate(validation);

        [Then(@"the page contains the element '(.*)'")]
        public void ThenThePageContains(Selector selector)
            => WebDriver.Select(selector).Should().NotBeNull();

        [Then(@"the table contains")]
        public void ThenTheTableContains(TableValidation table)
            => WebDriver.Tables.Validate(table);

        [Then(@"the element '(.*)' is '(.*)'")]
        public void ThenTheElementIs(Selector selector, WebValidation validation)
            => WebDriver.Select(selector).Validate(validation);
    }
}
