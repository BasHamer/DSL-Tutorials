using BoDi;
using System;
using System.IO;
using TechTalk.SpecFlow;

namespace LegacyTest.Steps
{
    [Binding]
    public class FrameworkInitializationSteps : StepBase
    {
        public FrameworkInitializationSteps(IObjectContainer objectContainer) : base(objectContainer)
        {

        }
        [BeforeScenario(Order = int.MinValue)]
        public void Setup()
        {
            var factory = new PossumLabs.Specflow.Core.Variables.ObjectFactory();
            base.Register(factory);
            base.Register(new PossumLabs.Specflow.Core.Variables.Interpeter(factory));
            base.Register(new PossumLabs.Specflow.Core.Exceptions.ActionExecutor());
            base.Register<PossumLabs.Specflow.Core.Logging.ILog>(new PossumLabs.Specflow.Core.Logging.DefaultLogger(new DirectoryInfo(Environment.CurrentDirectory)));
        }
    }
}