﻿using BoDi;
using Microsoft.Extensions.Configuration;
using PossumLabs.Specflow.Core;
using PossumLabs.Specflow.Core.Files;
using PossumLabs.Specflow.Core.Logging;
using System;
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
            ComparisonDefaults.StringComparison = StringComparison.InvariantCultureIgnoreCase;
        }
        [BeforeScenario(Order = int.MinValue)]
        public void Setup()
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .Build();
            var imageLoggingColor = config["imageLoggingColor"];
            var imageLoggingSize = config["imageLoggingSize"];

            ObjectContainer.RegisterInstanceAs(new ImageLogging());
            var factory = new PossumLabs.Specflow.Core.Variables.ObjectFactory();
            base.Register(factory);
            base.Register(new PossumLabs.Specflow.Core.Variables.Interpeter(factory));
            base.Register(new PossumLabs.Specflow.Core.Exceptions.ActionExecutor());
            base.Register<PossumLabs.Specflow.Core.Logging.ILog>(new PossumLabs.Specflow.Core.Logging.DefaultLogger(new DirectoryInfo(Environment.CurrentDirectory)));
            base.Register((PossumLabs.Specflow.Core.Logging.ILog)new DefaultLogger(new DirectoryInfo(Environment.CurrentDirectory)));
            base.Register(new FileManager(new DatetimeManager() { Now = () => DateTime.Now }));
            FileManager.Initialize(FeatureContext.FeatureInfo.Title, ScenarioContext.ScenarioInfo.Title, null /*Specflow limitation*/);
            var templateManager = new PossumLabs.Specflow.Core.Variables.TemplateManager();
            templateManager.Initialize(Assembly.GetExecutingAssembly());
            base.Register(templateManager);
        }

        [BeforeScenario(Order = 1)]
        public void SetupExistingData()
        {
            new PossumLabs.Specflow.Core.Variables.ExistingDataManager(this.Interpeter).Initialize(Assembly.GetExecutingAssembly());
        }
    }
}