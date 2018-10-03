using BoDi;
using PossumLabs.Specflow.Core;
using PossumLabs.Specflow.Core.Exceptions;
using PossumLabs.Specflow.Core.Logging;
using PossumLabs.Specflow.Core.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace LegacyTest
{
    public abstract class StepBase
    {
        public StepBase(IObjectContainer objectContainer)
        {
            ObjectContainer = objectContainer;

        }

        protected IObjectContainer ObjectContainer { get; }
        protected ScenarioContext ScenarioContext { get => ObjectContainer.Resolve<ScenarioContext>(); }
        protected FeatureContext FeatureContext { get => ObjectContainer.Resolve<FeatureContext>(); }


        protected Interpeter Interpeter => ScenarioContext.Get<Interpeter>((typeof(Interpeter).FullName));
        protected ActionExecutor Executor => ScenarioContext.Get<ActionExecutor>((typeof(ActionExecutor).FullName));
        protected ILog Log => ScenarioContext.Get<ILog>((typeof(ILog).FullName));
        protected ObjectFactory ObjectFactory => ScenarioContext.Get<ObjectFactory>(typeof(ObjectFactory).FullName);
        protected TemplateManager TemplateManager => ScenarioContext.Get<TemplateManager>(typeof(TemplateManager).FullName);

        internal void Register<T>(T item)
        {
            ScenarioContext.Add(typeof(T).FullName, item);
        }
    }
}
