using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Text;
using PossumLabs.Specflow.Core;
using System.Collections.ObjectModel;
using OpenQA.Selenium.Remote;
using System.Drawing;
using System.Linq;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Interactions;
using PossumLabs.Specflow.Core.Logging;
using System.Diagnostics;

namespace PossumLabs.Specflow.Selenium
{
#pragma warning disable CS0618 // Type or member is obsolete, 3rd party reference
    public class LoggingWebDriver : IWebDriver, ITakesScreenshot, IHasInputDevices, IActionExecutor
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public LoggingWebDriver(IWebDriver driver, MovieLogger movieLogger)
        {
            Driver = driver;
            Messages = new List<string>();
            Screenshots = new List<Screenshot>();
            MovieLogger = movieLogger;
        }

        private List<string> Messages { get; }
        private MovieLogger MovieLogger { get; }
        public List<Screenshot> Screenshots { get; }

        public string Url { get => Driver.Url; set => Driver.Url = value; }

        public string Title => Driver.Title;

        public string PageSource => Driver.PageSource;

        public string CurrentWindowHandle => Driver.CurrentWindowHandle;

        public ReadOnlyCollection<string> WindowHandles => Driver.WindowHandles;

#pragma warning disable CS0618 // Type or member is obsolete
        private IHasInputDevices HasInputDevices => (IHasInputDevices)Driver;
        public IKeyboard Keyboard => HasInputDevices.Keyboard;
        public IMouse Mouse => HasInputDevices.Mouse;
#pragma warning restore CS0618 // Type or member is obsolete

        private IActionExecutor ActionExecutor => (IActionExecutor)Driver;
        public bool IsActionExecutor => ActionExecutor.IsActionExecutor;

        private IWebDriver Driver;
        public string GetLogs() => Messages.LogFormat();

        public void Close() => Driver.Close();

        public void Quit() => Driver.Quit();

        public IOptions Manage() => Driver.Manage();

        public INavigation Navigate() => Driver.Navigate();

        public ITargetLocator SwitchTo() => Driver.SwitchTo();

        public IWebElement FindElement(By by)
        {
            Messages.Add(by.ToString());
            var element = Driver.FindElement(by);
            if (element != null && by.ToString().StartsWith("By.XPath: "))
                VisualLog(by);
            return element;
        }

        public ReadOnlyCollection<IWebElement> FindElements(By by)
        {
            Messages.Add(by.ToString());
            var elements = Driver.FindElements(by);
            if (elements != null && elements.Any() && by.ToString().StartsWith("By.XPath: "))
                VisualLog(by);
            return elements;
        }

        private void VisualLog(By by)
        {
            if (MovieLogger.IsEnabled)
            {
                var xpath = by.ToString().Substring("By.XPath: ".Length);
                var img = Preview(xpath);
                MovieLogger.AddScreenShot(img.AsByteArray);
                Screenshots.Add(img);
            }
        }

        public void Log(string message)
            => Messages.Add(message);

        public void Dispose() => Driver.Dispose();

        public Screenshot GetScreenshot()
            => ((ITakesScreenshot)Driver).GetScreenshot();

        //HACK: wrong place for this code
        private Screenshot Preview(string xpath)
        {
            var id = "data-possum-labs-"+Guid.NewGuid().ToString();
            //add style
            String s_Script = @"
var css = new function()
{
    function addStyleSheet()
    {
        let head = document.head;
        let style = document.createElement('style');

        head.appendChild(style);
    }

    this.insert = function(rule)
    {
        if(document.styleSheets.length == 0) { addStyleSheet(); }

        let sheet = document.styleSheets[document.styleSheets.length - 1];
        let rules = sheet.rules;

        sheet.insertRule(rule, rules.length);
    }
}

css.insert( '*['+arguments[0]+'] { outline: DarkOrange dashed 2px; }');
";
            ((IJavaScriptExecutor)Driver).ExecuteScript(s_Script, id);

            //mark items

            s_Script = @"
var nodesSnapshot = document.evaluate(arguments[1], document, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);

for (var i=0 ; i<nodesSnapshot.snapshotLength; i++ )
{
  nodesSnapshot.snapshotItem(i).setAttribute(arguments[0], i);
}
";
            ((IJavaScriptExecutor)Driver).ExecuteScript(s_Script, id, xpath);
            var screenshot = GetScreenshot();
            //remove style
            s_Script = @"
let sheet = document.styleSheets[document.styleSheets.length - 1];
let index = sheet.rules.length-1;
sheet.deleteRule(index);
";
            ((IJavaScriptExecutor)Driver).ExecuteScript(s_Script, id);
            //unmark
            s_Script = @"
var nodesSnapshot = document.evaluate('//*[@'+arguments[0]+']', document, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);

for (var i=0 ; i<nodesSnapshot.snapshotLength; i++ )
{
  nodesSnapshot.snapshotItem(i).removeAttribute(arguments[0]);
}
";
            ((IJavaScriptExecutor)Driver).ExecuteScript(s_Script, id);
            return screenshot;
        }
            
            
            
        /// <summary>
        /// Get the element at the viewport coordinates X, Y
        /// </summary>
        public RemoteWebElement GetElementFromPoint(int X, int Y)
        {
            while (true)
            {
                String s_Script = "return document.elementFromPoint(arguments[0], arguments[1]);";

                RemoteWebElement i_Elem = ((IJavaScriptExecutor)Driver).ExecuteScript(s_Script, X, Y) as RemoteWebElement;
                if (i_Elem == null)
                    return null;

                if (i_Elem.TagName != "frame" && i_Elem.TagName != "iframe")
                    return i_Elem;

                Point p_Pos = GetElementPosition(i_Elem);
                X -= p_Pos.X;
                Y -= p_Pos.Y;

                Driver.SwitchTo().Frame(i_Elem);
            }
        }

        //HACK: nested IFrames
        /// <summary>
        /// Get the position of the top/left corner of the Element in the document.
        /// NOTE: RemoteWebElement.Location is always measured from the top of the document and ignores the scroll position.
        /// </summary>
        public Point GetElementPosition(RemoteWebElement i_Elem)
        {
            String s_Script = "var X, Y; "
                            + "if (window.pageYOffset) " // supported by most browsers 
                            + "{ "
                            + "  X = window.pageXOffset; "
                            + "  Y = window.pageYOffset; "
                            + "} "
                            + "else " // Internet Explorer 6, 7, 8
                            + "{ "
                            + "  var  Elem = document.documentElement; "         // <html> node (IE with DOCTYPE)
                            + "  if (!Elem.clientHeight) Elem = document.body; " // <body> node (IE in quirks mode)
                            + "  X = Elem.scrollLeft; "
                            + "  Y = Elem.scrollTop; "
                            + "} "
                            + "return new Array(X, Y);";

            IList<Object> i_Coord = (IList<Object>)((IJavaScriptExecutor)Driver).ExecuteScript(s_Script);

            int s32_ScrollX = Convert.ToInt32(i_Coord[0]);
            int s32_ScrollY = Convert.ToInt32(i_Coord[1]);

            return new Point(i_Elem.Location.X - s32_ScrollX,
                             i_Elem.Location.Y - s32_ScrollY);

        }

        public void PerformActions(IList<ActionSequence> actionSequenceList)
        => ActionExecutor.PerformActions(actionSequenceList);

        public void ResetInputState()
        => ActionExecutor.ResetInputState();
    }
}
