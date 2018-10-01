using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Text;
using PossumLabs.Specflow.Core;
using System.Collections.ObjectModel;
using OpenQA.Selenium.Remote;
using System.Drawing;

namespace PossumLabs.Specflow.Selenium
{
    public class LoggingWebDriver : IWebDriver, ITakesScreenshot
    {
        public LoggingWebDriver(IWebDriver driver)
        {
            Driver = driver;
            Messages = new List<string>();
        }

        private List<string> Messages { get; }
        public string Url { get => Driver.Url; set => Driver.Url = value; }

        public string Title => Driver.Title;

        public string PageSource => Driver.PageSource;

        public string CurrentWindowHandle => Driver.CurrentWindowHandle;

        public ReadOnlyCollection<string> WindowHandles => Driver.WindowHandles;

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
            return Driver.FindElement(by);
        }

        public ReadOnlyCollection<IWebElement> FindElements(By by)
        {
            Messages.Add(by.ToString());
            return Driver.FindElements(by);
        }

        public void Log(string message)
            => Messages.Add(message);

        public void Dispose() => Driver.Dispose();

        public Screenshot GetScreenshot()
            => ((ITakesScreenshot)Driver).GetScreenshot();



        //HACK: wrong place for this code
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
    }
}
