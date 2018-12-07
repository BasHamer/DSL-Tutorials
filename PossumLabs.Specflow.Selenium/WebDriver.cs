using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using PossumLabs.Specflow.Core;
using PossumLabs.Specflow.Core.Exceptions;
using PossumLabs.Specflow.Core.Variables;
using PossumLabs.Specflow.Selenium.Configuration;
using PossumLabs.Specflow.Selenium.Selectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PossumLabs.Specflow.Selenium
{
    public class WebDriver : IDisposable, IDomainObject
    {
        public WebDriver(
            IWebDriver driver, 
            Func<Uri> rootUrl, 
            SeleniumGridConfiguration configuration, 
            RetryExecutor retryExecutor, 
            SelectorFactory selectorFactory,
            IEnumerable<SelectorPrefix> prefixes = null)
        {
            Driver = driver;
            SuccessfulSearchers = new List<Searcher>();
            RootUrl = rootUrl;
            SeleniumGridConfiguration = configuration;
            RetryExecutor = retryExecutor;
            SelectorFactory = selectorFactory;
            Prefixes = prefixes?.ToList() ?? new List<SelectorPrefix>() { new EmptySelectorPrefix() };

            Children = new List<WebDriver>();
            Screenshots = new List<byte[]>();
        }

        private Func<Uri> RootUrl { get; set; }
        private IWebDriver Driver { get; }
        private SelectorFactory SelectorFactory { get; }
        private List<Searcher> SuccessfulSearchers { get; }

        public IEnumerable<TableElement> GetTables(int minimumWidth)
        {
            var xpath = $"//tr[*[self::td or self::th][{minimumWidth}] and (.|parent::tbody)[1]/parent::table]/ancestor::table[1]";
            var tables = Driver.
                FindElements(By.XPath(xpath))
                .Select(t => new TableElement(t, Driver)).ToList();
            var Ordinal = 1;
            foreach (var table in tables)
            {
                table.Ordinal = Ordinal++;
                table.Xpath = xpath;
                table.Setup();
            }
            return tables;
        }

        private SeleniumGridConfiguration SeleniumGridConfiguration { get; }
        private RetryExecutor RetryExecutor { get; }
        private List<SelectorPrefix> Prefixes { get; }
        private List<byte[]> Screenshots { get; set; }
        private List<WebDriver> Children { get; set; }

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

        public Actions BuildAction()
            => new Actions(Driver);

        public void LoadPage(string html)
        {
            Driver.Navigate().GoToUrl("data:text/html;charset=utf-8," + html);
        }

        public void SwitchToWindow(string window)
            => Driver.SwitchTo().Window(window);

        public Element Select(Selector selector)
            => RetryExecutor.RetryFor(() =>
             {
                 var loggingWebdriver = new LoggingWebDriver(Driver);
                 try
                 {
                     var element = FindElement(selector, loggingWebdriver);
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
                             element = FindElement(selector, loggingWebdriver);
                             if (element != null)
                                 return element;
                         }
                         catch
                         { }
                     }
                     Driver.SwitchTo().DefaultContent();
                     throw new Exception($"element was not found; tried:\n{loggingWebdriver.GetLogs()}, maybe try one of these identifiers {GetIdentifiers().LogFormat()}");
                 }
                 finally
                 {
                     if (loggingWebdriver.Screenshots.Any())
                         Screenshots = loggingWebdriver.Screenshots.Select(x => x.AsByteArray).ToList();

                 }
             }, TimeSpan.FromMilliseconds(SeleniumGridConfiguration.RetryMs));

        public void Close()
            => Driver.Close();

        public IEnumerable<Element> SelectMany(Selector selector)
            => RetryExecutor.RetryFor(() =>
            {
                var loggingWebdriver = new LoggingWebDriver(Driver);
                try
                {
                    var elements = FindElements(selector, loggingWebdriver);
                    if (elements != null)
                        return elements;
                    //iframes ? 
                    var iframes = Driver.FindElements(By.XPath("//iframe"));
                    foreach (var iframe in iframes)
                    {
                        try
                        {
                            loggingWebdriver.Log($"Trying iframe:{iframe}");
                            Driver.SwitchTo().Frame(iframe);
                            elements = FindElements(selector, loggingWebdriver);
                            if (elements != null)
                                return elements;
                        }
                        catch
                        { }
                    }
                    Driver.SwitchTo().DefaultContent();
                    throw new Exception($"element was not found; tried:\n{loggingWebdriver.GetLogs()}, maybe try one of these identifiers {GetIdentifiers().LogFormat()}");
                }
                finally
                {
                    if (loggingWebdriver.Screenshots.Any())
                        Screenshots = loggingWebdriver.Screenshots.Select(x => x.AsByteArray).ToList();

                }
            }, TimeSpan.FromMilliseconds(SeleniumGridConfiguration.RetryMs));

        public void DismissAllert()
            => Driver.SwitchTo().Alert().Dismiss();

        public void AcceptAllert()
            => Driver.SwitchTo().Alert().Accept();

        private class Wrapper
        {
            public IEnumerable<Element> Elements;
            public Element Element;
            public Searcher Searcher;
            public Exception Exception;
        }

        private Element FindElement(Selector selector, LoggingWebDriver loggingWebdriver)
        {
            var wrappers = selector.PrioritizedSearchers.Select(s => new Wrapper { Searcher = s }).ToList();
            var loopResults = Parallel.ForEach(wrappers, (wrapper, loopState) =>
            {
                var searcher = wrapper.Searcher;
                var results = searcher.SearchIn(loggingWebdriver, Prefixes);

                if (results.One())
                {
                    SuccessfulSearchers.Add(searcher);
                    loopState.Break();
                    wrapper.Element = results.First();
                    return;
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
                        loopState.Break();
                        wrapper.Element = results.First();
                        return;
                    }

                    var items = results.Select(e => $"{e.Tag}@{e.Location.X},{e.Location.Y}").LogFormat();
                    loopState.Break();
                    wrapper.Exception = new Exception($"Multiple results were found using {searcher.LogFormat()}");
                    return;
                }
            });
            var r = loopResults.IsCompleted;
            var index = 0;
            foreach (var w in wrappers)
            {
                if (w.Element != null)
                    return w.Element;
                if (w.Exception != null)
                    throw new AggregateException($"Error throw on xpath {index}", w.Exception);
                index++;
            }
            return null;
        }

        private IEnumerable<Element> FindElements(Selector selector, LoggingWebDriver loggingWebdriver)
        {
            var wrappers = selector.PrioritizedSearchers.Select(s => new Wrapper { Searcher = s }).ToList();
            var loopResults = Parallel.ForEach(wrappers, (wrapper, loopState) =>
            {
                var searcher = wrapper.Searcher;
                var results = searcher.SearchIn(loggingWebdriver, Prefixes);

                SuccessfulSearchers.Add(searcher);
                loopState.Break();
                wrapper.Elements = results;
                return;
            });
            var r = loopResults.IsCompleted;
            var index = 0;
            foreach (var w in wrappers)
            {
                if (w.Element != null)
                    return w.Elements;
                if (w.Exception != null)
                    throw new AggregateException($"Error throw on xpath {index}", w.Exception);
                index++;
            }
            return null;
        }

        virtual public List<string> GetIdentifiers()
        {
            var options = new List<Tuple<By, Func<IWebElement, string>, List<string>>>()
            {
                new Tuple<By, Func<IWebElement,string>, List<string>>(
                    By.XPath("//label"), (e)=>e.Text,  new List<string>()),
                new Tuple<By, Func<IWebElement,string>, List<string>>(
                    By.XPath("//*[self::button or self::a or (self::input and @type='button')]"), (e)=>e.Text, new List<string>()),
                new Tuple<By, Func<IWebElement,string>, List<string>>(
                    By.XPath("//*[@alt]"), (e)=>e.GetAttribute("alt"),  new List<string>()),
                new Tuple<By, Func<IWebElement,string>, List<string>>(
                    By.XPath("//*[@name]"), (e)=>e.GetAttribute("name"),  new List<string>()),
                new Tuple<By, Func<IWebElement,string>, List<string>>(
                    By.XPath("//*[@aria-label]"), (e)=>e.GetAttribute("aria-label"),  new List<string>()),
                new Tuple<By, Func<IWebElement,string>, List<string>>(
                    By.XPath("//*[@title]"), (e)=>e.GetAttribute("title"),  new List<string>()),
            };

            Parallel.ForEach(options, (option, loopState) =>
            {
                var elements = Driver.FindElements(option.Item1);
                foreach (var e in elements)
                    option.Item3.Add(option.Item2(e));
            });

            return options.SelectMany(o => o.Item3).Distinct().OrderBy(s => s.ToLower()).ToList();
        }

        public void ExecuteScript(string script)
            => ((IJavaScriptExecutor)Driver).ExecuteScript(script);

        public WebDriver Under(UnderSelectorPrefix under)
            => Prefix(under);

        public WebDriver ForRow(RowSelectorPrefix row)
            => Prefix(row);

        public WebDriver ForError()
            => Prefix(SelectorFactory.CreatePrefix<ErrorSelectorPrefix>());

        public WebDriver ForWarning()
            => Prefix(SelectorFactory.CreatePrefix<WarningSelectorPrefix>());

        public WebDriver Prefix(SelectorPrefix prefix)
        {
            var wdm = new WebDriver(Driver, RootUrl, SeleniumGridConfiguration, RetryExecutor, SelectorFactory, Prefixes.Concat(prefix));
            Children.Add(wdm);
            return wdm;
        }

        public void Dispose()
            => Driver.Dispose();

        public IEnumerable<byte[]> GetScreenshots()
        {
            foreach (var c in Children)
                foreach (var s in c.GetScreenshots())
                    yield return s;
            foreach (var s in Screenshots)
                yield return s;
            yield return ((ITakesScreenshot)Driver).GetScreenshot().AsByteArray;
        }

        public void ResetScreenshots()
        {
            Children = new List<WebDriver>();
            Screenshots = new List<byte[]>();
        }

        public string LogFormat() => Url;

        public string PageSource { get => Driver.PageSource; }
        public string Url { get => Driver.Url; }
        public IEnumerable<string> Windows { get => Driver.WindowHandles; }
        public string AlertText
        {
            get
            {
                try
                {
                    return Driver.SwitchTo().Alert().Text;

                }
                catch
                {
                    return null;
                }
            }
        }

        public Size Size
        {
            get => Driver.Manage().Window.Size;
            set => Driver.Manage().Window.Size = value;
        }
        public bool HasAlert
        {
            get {
                try {
                    Driver.SwitchTo().Alert();
                    return true;
                }
                catch {
                    return false;
                }
            }
        }

     
    }
}

