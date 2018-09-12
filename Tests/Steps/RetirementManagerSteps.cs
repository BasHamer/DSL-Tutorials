using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoDi;
using LegacyTest.DomainObjects;
using LegacyTest.Managers;
using TechTalk.SpecFlow;

namespace LegacyTest.Steps
{
    [Binding]
    public class RetirementManagerSteps:StepBase
    {
        public RetirementManagerSteps(IObjectContainer objectContainer) : base(objectContainer)
        {
            RetirementManager = new RetirementManager();
            Register<RetirementManager>(RetirementManager);
        }
        private RetirementManager RetirementManager { get; }

        [Given(@"the root Employee is '(.*)'")]
        public void GivenTheRootEmployeeIs(Employee ceo)
            =>RetirementManager.SetCEO(ceo);


        [When(@"Employee '(.*)' Retires")]
        public void WhenEmployeeRetires(Employee retiree)
            => RetirementManager.Retire(retiree);
    }
}
