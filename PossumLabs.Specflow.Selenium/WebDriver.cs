using OpenQA.Selenium;
using PossumLabs.Specflow.Core;
using PossumLabs.Specflow.Core.Exceptions;
using PossumLabs.Specflow.Selenium.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PossumLabs.Specflow.Selenium
{
    public class WebDriver:IDisposable
    {
        public WebDriver(IWebDriver driver, Func<Uri> rootUrl, SeleniumGridConfiguration configuration, RetryExecutor retryExecutor, IEnumerable<SelectorPrefix> prefixes = null)
        {
            Driver = driver;
            SuccessfulSearchers = new List<Searcher>();
            RootUrl = rootUrl;
            SeleniumGridConfiguration = configuration;
            RetryExecutor = retryExecutor;
            Prefixes = prefixes ?? new List<SelectorPrefix>() { new EmptySelectorPrefix() };
        }

        private Func<Uri> RootUrl {get;set;}
        private IWebDriver Driver { get; }
        private List<Searcher> SuccessfulSearchers { get; }
        private SeleniumGridConfiguration SeleniumGridConfiguration { get; }
        private RetryExecutor RetryExecutor { get; }
        private IEnumerable<SelectorPrefix> Prefixes;

        //TODO: check this form
        public void NavigateTo(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
                Driver.Navigate().GoToUrl(url);
            else
                Driver.Navigate().GoToUrl(RootUrl().AbsoluteUri + url);
        }

        public void LeaveFrames()
            => Driver.SwitchTo().DefaultContent();

        public void LoadPage(string html)
        {
            Driver.Navigate().GoToUrl("data:text/html;charset=utf-8," + html);
        }

        public Element Select(Selector selector)
            =>RetryExecutor.RetryFor(() =>
            {
                var loggingWebdriver = new LoggingWebDriver(Driver);
                var index = 0;
                var element = FindElement(selector, loggingWebdriver, ref index);
                if (element != null)
                    return element;
                //iframes ? 
                var iframes = Driver.FindElements(By.XPath("//iframe"));
                foreach (var iframe in iframes)
                {
                    try
                    {
                        loggingWebdriver.Log($"Trying iframe:{iframe}");
                        Driver.SwitchTo().Frame(iframe);
                        element = FindElement(selector, loggingWebdriver, ref index);
                        if (element != null)
                            return element;
                    }
                    catch
                    { }
                }
                Driver.SwitchTo().DefaultContent();
                throw new Exception($"element was not found; tried:\n{loggingWebdriver.GetLogs()}");
            }, TimeSpan.FromMilliseconds(SeleniumGridConfiguration.RetryMs));
        

        private Element FindElement(Selector selector, LoggingWebDriver loggingWebdriver, ref int index)
        {
            foreach (var searcher in selector.PrioritizedSearchers)
            {
                var results = searcher.SearchIn(loggingWebdriver, Prefixes);

                if (results.One())
                {
                    SuccessfulSearchers.Add(searcher);
                    return results.First();
                }
                else if (results.Many())
                {
                    //HACK: HACK HELL
                    var filterHidden = results
                        .Select(e => new { e, o = loggingWebdriver.GetElementFromPoint(e.Location.X + 1, e.Location.Y + 1) })
                        .Where(p => p.e.Tag == p.o?.TagName && p.e.Location == p.o?.Location);
                    if (filterHidden.One())
                    {
                        SuccessfulSearchers.Add(searcher);
                        return results.First();
                    }

                    var items = results.Select(e => $"{e.Tag}@{e.Location.X},{e.Location.Y}").LogFormat();
                    throw new Exception($"Multiple results were found using {searcher.LogFormat()} using seracher {index}");
                }
                index++;
            }
            return null;
        }

        public void ExecuteScript(string script)
            => ((IJavaScriptExecutor)Driver).ExecuteScript(script);
               
        public WebDriver Under(UnderSelectorPrefix under)
            => new WebDriver(Driver, RootUrl, SeleniumGridConfiguration, RetryExecutor, Prefixes.Concat(under));

        public WebDriver ForRow(RowSelectorPrefix row)
            => new WebDriver(Driver, RootUrl, SeleniumGridConfiguration, RetryExecutor, Prefixes.Concat(row));

        public void Dispose()
            => Driver.Dispose();

        public byte[] Screenshot()
            => ((ITakesScreenshot)Driver).GetScreenshot().AsByteArray;


        public IEnumerable<TableElement> Tables
            => Driver.FindElements(By.TagName("table")).Select(t => new TableElement(t, Driver));

        public string PageSource { get => Driver.PageSource; }
    }
}
