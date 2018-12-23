using BoDi;
using LegacyTest;
using PossumLabs.Specflow.Core.Exceptions;
using PossumLabs.Specflow.Core.Logging;
using PossumLabs.Specflow.Selenium;
using PossumLabs.Specflow.Selenium.Configuration;
using PossumLabs.Specflow.Selenium.Diagnostic;
using PossumLabs.Specflow.Selenium.Selectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Shim.Selenium
{
    [Binding]
    public class FrameworkInitializationSteps : StepBase
    {
        public FrameworkInitializationSteps(IObjectContainer objectContainer) : base(objectContainer)
        {
            WebDriverManager = new PossumLabs.Specflow.Selenium.WebDriverManager( 
                this.Interpeter,
                this.ObjectFactory,
                new PossumLabs.Specflow.Selenium.Configuration.SeleniumGridConfiguration() );
            WebDriverManager.Initialize(BuildDriver);
            SelectorFactory = new LegacyTest.Framework.SpecializedSelectorFactory();
        }

        private SelectorFactory SelectorFactory { get; }

        public WebDriver BuildDriver()
            => new WebDriver(
                WebDriverManager.Create(),
                () => WebDriverManager.BaseUrl,
                ObjectContainer.Resolve<SeleniumGridConfiguration>(),
                ObjectContainer.Resolve<RetryExecutor>(),
                SelectorFactory);

        private PossumLabs.Specflow.Selenium.WebDriverManager WebDriverManager { get; }
        private ScreenshotProcessor ScreenshotProcessor { get; set; }
        private ImageLogging ImageLogging { get; set; }

        [BeforeScenario(Order = int.MinValue+1)]
        public void Setup()
        {
            ScreenshotProcessor = ObjectContainer.Resolve<ScreenshotProcessor>();
            ImageLogging = ObjectContainer.Resolve<ImageLogging>();

            base.Register(WebDriverManager);
            base.Register(new PossumLabs.Specflow.Selenium.WebValidationFactory(Interpeter));
            base.Register<SelectorFactory>(SelectorFactory);
        }

        [AfterStep]
        public void TakeScreenshot()
        {
            if (WebDriverManager.ActiveDriver)
            {
                foreach (var img in WebDriverManager.Current.GetScreenshots())
                {
                    var withText = ImageLogging.AddTextToImage(img, $"{ScenarioContext.StepContext.StepInfo.StepDefinitionType} {ScenarioContext.StepContext.StepInfo.Text}");
                    WebDriverManager.Screenshots.Add(withText);
                }

                WebDriverManager.Current.ResetScreenshots();
            }
        }

        [AfterScenario]
        public void LogScreenshots()
        {
            if (WebDriverManager.Screenshots.Any())
            {
                //This is a seperation of concerns issue; not sure how to fix it yet w/o a package update for the gif code. 
                string random = "temp";
                ScreenshotProcessor.CreateGif($"{random}.gif", WebDriverManager.Screenshots);
                FileManager.CreateFile($"{random}.gif", "movie", "gif");
            }
        }

        [AfterScenario(Order =int.MaxValue)]
        public void DisposeDriverManager()
        {
            WebDriverManager.Dispose();
        }

        [AfterScenario(Order = int.MinValue)]
        public void LogHtml()
        {
            if (WebDriverManager.ActiveDriver)
            {
                FileManager.CreateFile(Encoding.UTF8.GetBytes(WebDriverManager.Current.PageSource), "source", "html");
            }
        }
    }
}
