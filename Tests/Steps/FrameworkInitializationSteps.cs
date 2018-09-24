using BoDi;
using PossumLabs.Specflow.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            base.Register((PossumLabs.Specflow.Core.Logging.ILog)new DefaultLogger(new DirectoryInfo(Environment.CurrentDirectory)));
            var templateManager = new PossumLabs.Specflow.Core.Variables.TemplateManager();
            templateManager.Initialize(Assembly.GetExecutingAssembly());
            base.Register(templateManager);
        }
    }
}