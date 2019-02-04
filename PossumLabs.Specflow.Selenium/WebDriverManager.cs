using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using PossumLabs.Specflow.Core.Variables;
using PossumLabs.Specflow.Selenium.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace PossumLabs.Specflow.Selenium
{
    public class WebDriverManager:RepositoryBase<WebDriver>, IDisposable
    {
        public WebDriverManager(
            Interpeter interpeter, 
            ObjectFactory objectFactory, 
            SeleniumGridConfiguration seleniumGridConfiguration) : 
            base(interpeter, objectFactory)
        {
            SeleniumGridConfiguration = seleniumGridConfiguration;
            Screenshots = new List<byte[]>();
            DefaultDriver = new Lazy<WebDriver>(()=>DefaultDriverFactory());
            Drivers = new List<RemoteWebDriver>();
            DefaultSize = new System.Drawing.Size(SeleniumGridConfiguration.Width, SeleniumGridConfiguration.Height);
        }


        public void Initialize(Func<WebDriver> defaultDriverFactory)
        {
            DefaultDriverFactory = defaultDriverFactory;
        }
        public SeleniumGridConfiguration SeleniumGridConfiguration { get; }
        public WebDriver Current
        {
            get => OverWrittenDriver ?? DefaultDriver.Value;
        }
        public void SetCurrentDriver(WebDriver webdriver)
        { OverWrittenDriver = webdriver; }
        private WebDriver OverWrittenDriver { get; set; }
        private Lazy<WebDriver> DefaultDriver { get; set; }
        public Func<WebDriver> DefaultDriverFactory { get; private set; }
        public Uri BaseUrl { get; set; }

        public bool ActiveDriver => OverWrittenDriver != null || DefaultDriver.IsValueCreated;

        public List<byte[]> Screenshots { get; }

        public Func<IWebDriver> Create => () =>
        {
            var options = new ChromeOptions();
            // https://stackoverflow.com/questions/22322596/selenium-error-the-http-request-to-the-remote-webdriver-timed-out-after-60-sec?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa
            options.AddArgument("no-sandbox"); //might be a fix :/
            options.AddArgument("disable-popup-blocking");
            //TODO: Config value
            var driver = new RemoteWebDriver(new Uri(SeleniumGridConfiguration.Url), options.ToCapabilities(), TimeSpan.FromSeconds(180));
            //do not change this, the site is a bloody nightmare with overlaying buttons etc.
            driver.Manage().Window.Size = DefaultSize;
            var allowsDetection = driver as IAllowsFileDetection;
            if (allowsDetection != null)
            {
                allowsDetection.FileDetector = new LocalFileDetector();
            }
            Drivers.Add(driver);
            return driver;
        };

        public Size DefaultSize { get; set; }
        public bool IsInitialized => DefaultDriver.IsValueCreated;

        private List<RemoteWebDriver> Drivers;

        public DisposableDriverScope DisposableDriver()
        {
            var c = OverWrittenDriver;
            OverWrittenDriver = DefaultDriverFactory();
            return new DisposableDriverScope(
                () =>
                {
                    var d = ((IWebDriverWrapper)OverWrittenDriver).IWebDriver;
                    d.Quit();
                    d.Dispose();
                    OverWrittenDriver.Disposed = true;
                    OverWrittenDriver = c;
                });
        }

        public void Dispose()
        {
            foreach (var d in Drivers)
            {
                try
                {
                    d.Quit();
                    d.Dispose();
                }
                catch { }
            }
        }

        public class DisposableDriverScope : IDisposable
        {
            public DisposableDriverScope(Action disposal)
            {
                Disposal = disposal;
            }
            Action Disposal { get; }
            public void Dispose()
                => Disposal();
        }
    }
}
