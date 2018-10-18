using OpenQA.Selenium;
using PossumLabs.Specflow.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PossumLabs.Specflow.Selenium
{
    public class WebDriver:IDisposable
    {
        public WebDriver(IWebDriver driver, Func<Uri> rootUrl, IEnumerable<SelectorPrefix> prefixes = null)
        {
            Driver = driver;
            SuccessfulSearchers = new List<Searcher>();
            RootUrl = rootUrl;
            Prefixes = prefixes ?? new List<SelectorPrefix>() { new EmptySelectorPrefix() };
        }

        private Func<Uri> RootUrl {get;set;}
        private IWebDriver Driver { get; }
        private List<Searcher> SuccessfulSearchers { get; }
        private IEnumerable<SelectorPrefix> Prefixes;

        //TODO: check this form
        public void NavigateTo(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
                Driver.Navigate().GoToUrl(url);
            else
                Driver.Navigate().GoToUrl(RootUrl().AbsoluteUri + url);
        }

        public void LoadPage(string html)
        {
            Driver.Navigate().GoToUrl("data:text/html;charset=utf-8," + html);
        }

        public Element Select(Selector selector)
        {
            var loggingWebdriver = new LoggingWebDriver(Driver);
            var index = 0;
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
                        .Select(e=>new { e, o = loggingWebdriver.GetElementFromPoint(e.Location.X + 1, e.Location.Y + 1) })
                        .Where(p => p.e.Tag == p.o?.TagName && p.e.Location == p.o?.Location);
                    if(filterHidden.One())
                    {
                        SuccessfulSearchers.Add(searcher);
                        return results.First();
                    }

                    var items = results.Select(e => $"{e.Tag}@{e.Location.X},{e.Location.Y}").LogFormat();
                    throw new Exception($"Multiple results were found using {searcher.LogFormat()} using seracher {index}");
                }
                index++;
            }
            throw new Exception($"element was not found; tried:\n{loggingWebdriver.GetLogs()}");
        }

        public void ExecuteScript(string script)
            => ((IJavaScriptExecutor)Driver).ExecuteScript(script);
               
        public WebDriver Under(UnderSelectorPrefix under)
            => new WebDriver(Driver, RootUrl, Prefixes.Concat(under));

        public WebDriver ForRow(RowSelectorPrefix row)
            => new WebDriver(Driver, RootUrl, Prefixes.Concat(row));

        public void Dispose()
            => Driver.Dispose();

        public byte[] Screenshot()
            => ((ITakesScreenshot)Driver).GetScreenshot().AsByteArray;


        public IEnumerable<TableElement> Tables
            => Driver.FindElements(By.TagName("table")).Select(t => new TableElement(t, Driver));

        public string PageSource { get => Driver.PageSource; }
    }
}
