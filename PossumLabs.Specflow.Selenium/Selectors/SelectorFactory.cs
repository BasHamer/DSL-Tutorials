using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using PossumLabs.Specflow.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PossumLabs.Specflow.Selenium.Selectors
{
    public class SelectorFactory
    {
        public SelectorFactory()
        {
            Selectors = new Dictionary<string, List<Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>>>>
            {
                {
                    SelectorNames.Active,
                    new List<Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>>>
                    {
                        ByForAttribute,
                        ByNestedInLabel,
                        ByNested,
                        ByText,
                        ByTitle,
                        ByLabelledBy,
                        RadioByName,
                        SpecialButtons,
                        ByFollowingMarker,
                        ByCellBelow,
                    }
                },
                {
                    SelectorNames.Content,
                    new List<Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>>>
                    {
                        ByContentSelf,
                        ByContent
                    }
                }
            };

            Prefixes = new Dictionary<string, List<Func<string, IEnumerable<string>>>>
            {
                {
                    PrefixNames.Row,
                    new List<Func<string, IEnumerable<string>>>
                    {
                        TableRow,
                        DivRoleRow,
                    }
                },
                {
                    PrefixNames.Under,
                    new List<Func<string, IEnumerable<string>>>
                    {
                        ParrentDiv,
                        ParrentDivWithRowRole,
                        FollowingRow
                    }
                },
                { PrefixNames.Warning, new List<Func<string, IEnumerable<string>>> { } },
                { PrefixNames.Error, new List<Func<string, IEnumerable<string>>> { } }
            };
        }

        protected static readonly Core.EqualityComparer<IWebElement> Comparer =
            new Core.EqualityComparer<IWebElement>((x, y) => x.Location == y.Location && x.TagName == y.TagName);



        public T CreateSelector<T>(string constructor) where T: Selector,new()
        {
            var t = new T();
            if (Parser.IsId.IsMatch(constructor))
                t.Init(Parser.IsId.Match(constructor).Groups[1].Value,
                    new List<Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>>> { ById });
            else if (Parser.IsElement.IsMatch(constructor))
                t.Init(Parser.IsElement.Match(constructor).Groups[1].Value, 
                    new List<Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>>>{ ByTag });
            else if (Parser.IsClass.IsMatch(constructor))
                t.Init(Parser.IsClass.Match(constructor).Groups[1].Value,
                    new List<Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>>> { ByClass });
            else if (Selectors.ContainsKey(t.Type) && Selectors[t.Type].Any())
                t.Init(constructor, Selectors[t.Type]);
            else
                throw new GherkinException($"the selector type of '{t.Type}' is not supported.");
            return t;
        }

        public T CreatePrefix<T>(string constructor = "") where T: SelectorPrefix,new()
        {
            var t = new T();
            if (Parser.IsId.IsMatch(constructor))
                t.Init(constructor, $"//*[@id='{Parser.IsId.Match(constructor).Groups[1].Value}']");
            else if (Parser.IsElement.IsMatch(constructor))
                t.Init(constructor, $"//{Parser.IsElement.Match(constructor).Groups[1].Value}");
            else if (Parser.IsClass.IsMatch(constructor))
                t.Init(constructor, $"//*[contains(concat(' ', normalize-space(@class), ' '), ' {Parser.IsClass.Match(constructor).Groups[1].Value} ')]");
            else if (Prefixes.ContainsKey(t.Type) && Prefixes[t.Type].Any())
                t.Init(constructor, Prefixes[t.Type]);
            else
                throw new GherkinException($"the prefix type of '{t.Type}' is not supported.");
            return t;
        }

        protected IEnumerable<Element> Permutate(IEnumerable<SelectorPrefix> prefixes, IWebDriver driver, Func<string, string> xpathMaker)
            => prefixes.CrossMultiply().Select(prefix =>
                    driver
                        .FindElements(By.XPath(xpathMaker(prefix)))
                        .Where(Filter)
                        .Distinct(Comparer)
                        .Select(e => CreateElement(driver, e))
                ).FirstOrDefault(e => e.Any()) ?? new Element[] { };

        public Dictionary<string, List<Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>>>> Selectors { get; }
        public Dictionary<string, List<Func<string, IEnumerable<string>>>> Prefixes { get; }

        protected bool Filter(IWebElement e) =>
            e is RemoteWebElement && ((RemoteWebElement)e).Displayed && ((RemoteWebElement)e).Enabled;

        virtual protected Element CreateElement(IWebDriver driver, IWebElement e)
        {
            if (e.TagName == "select" || (e.TagName == "input" && !string.IsNullOrEmpty(e.GetAttribute("list"))))
                return new SelectElement(e, driver);
            if (e.TagName == "input" && e.GetAttribute("type") == "radio")
            {
                var elements = driver.FindElements(By.XPath($"//input[@type='radio' and @name='{e.GetAttribute("name")}']"));
                return new RadioElement(elements, driver);
            }
            if (e.TagName == "input" && e.GetAttribute("type") == "checkbox")
            {
                return new CheckboxElement(e, driver);
            }

            return new Element(e, driver);
        }

        #region Selectors
        //https://w3c.github.io/using-aria/

        //<label for="female">target</label>
        //label[@for and text()='{target}']
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByForAttribute =>
            (target, prefixes, driver) =>
            {
                foreach (var prefix in prefixes.CrossMultiply())
                {
                    var elements = driver.FindElements(By.XPath($"{prefix}//label[@for and {TextMatch(target)}]"));
                    if (elements.Any())
                        return elements.SelectMany(e => driver.FindElements(By.Id(e.GetAttribute("for"))))
                        .Select(e => CreateElement(driver, e));
                }
                return new Element[] { };
            };

        //label[text()[normalize-space(.)='Bob']]/*[self::input]
        //<label>target<input type = "text" ></ label >
        //label[text()='{target}']/*[self::input or self::textarea or self::select]
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByNestedInLabel =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//label[{TextMatch(target)}]/*[{ActiveElements}]");

        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> SpecialButtons =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//*[(self::input or self::button) and @type={target.XpathEncode()} and (@type='submit' or @type='reset')]");

        //<input aria-label="target">
        //*[(self::a or self::button or @role='button' or @role='link' or @role='menuitem' or self::input or self::textarea or self::select) and @aria-label='{target}']
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByNested =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//*[{ActiveElements} and (" +
                    $"{TextMatch(target)} or " +
                    $"label[{TextMatch(target)}] or " +
                    $"((@type='button' or @type='submit' or @type='reset') and @value={target.XpathEncode()}) or " +
                    $"@name={target.XpathEncode()} or " +
                    $"@aria-label={target.XpathEncode()} or " +
                    $"(@type='radio' and @value={target.XpathEncode()})" +
                $")]");

        //<a href = "https://www.w3schools.com/html/" >target</a>
        //*[(self::a or self::button or @role='button' or @role='link' or @role='menuitem') and text()='{target}']
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByText =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//*[{ActiveElements} and {TextMatch(target)}]");

        //<a href = "https://www.w3schools.com/html/" title="target">Visit our HTML Tutorial</a>
        //a[@title='{target}']
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByTitle =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//a[@title={target.XpathEncode()}]");

        //<input type="radio" id="i1" name="target"
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> RadioByName =>
            (target, prefixes, driver) =>
            {
                foreach (var prefix in prefixes.CrossMultiply())
                {
                    var elements = driver.FindElements(By.XPath($"{prefix}//input[@type='radio' and @name={target.XpathEncode()}]"));
                    if (elements.Any())
                        return new Element[] { new RadioElement(elements, driver) };
                }
                return new Element[] { };
            };

        //<input aria-labelledby= "l1 l2 l3"/>
        //*[(self::a or self::button or @role='button' or @role='link' or @role='menuitem' or self::input or self::textarea or self::select) and  @aria-labelledby]
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByLabelledBy =>
            (target, prefixes, driver) =>
            {
                foreach (var prefix in prefixes.CrossMultiply())
                {
                    var elements = driver.FindElements(By.XPath($"{prefix}//*[{ActiveElements} and  @aria-labelledby]"));
                    if (elements.Any())
                    {
                        return elements.Where(e =>
                        {
                            var ids = e.GetAttribute("aria-labelledby").Split(' ').Select(s => s.Trim()).Where(s => !String.IsNullOrEmpty(s));
                            var labels = ids.SelectMany(id => driver.FindElements(By.Id(id))).Select(l => l.Text);
                            var t = target;
                            foreach (var l in labels)
                                t = t.Replace(l, string.Empty);
                            return string.IsNullOrWhiteSpace(t);
                        }).Select(e => CreateElement(driver, e));
                    }
                }
                return new Element[] { };
            };
        // //tr[*[self::td][*[( self::span ) and text()[normalize-space(.)='Add Clinic']]]]/following-sibling::tr[1]/td[1+count(//*[self::td][*[( self::span ) and text()[normalize-space(.)='Add Clinic']]]/preceding-sibling::*[self::td])]
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByCellBelow =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//tr[*[self::td or self::th][*[{MarkerElements} and {TextMatch(target)}]]]/following-sibling::tr[1]/td[1+count(//*[self::td or self::th][*[{MarkerElements} and {TextMatch(target)}]]/preceding-sibling::*[self::td or self::th])]/*[{ActiveElements}]");

        //<a href = "https://www.w3schools.com/html/" title="target">Visit our HTML Tutorial</a>
        //a[@title='{target}']
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByFollowingMarker =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//*[{MarkerElements} and {TextMatch(target)}]/following-sibling::*[not(self::br or self::hr)][1][{ActiveElements}]");

        #endregion

        #region content
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByContentSelf =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                string.IsNullOrWhiteSpace(prefix) ?
                    "//*[1=2]" : //junk, valid xpath that never returns anything. used for prefixes.
                    $"{prefix}/self::*[{ContentElements} and {TextMatch(target)}]");


        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByContent =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//*[{ContentElements} and {TextMatch(target)}]");

        #endregion


        #region id & class & tag selectors
        //https://stackoverflow.com/questions/1604471/how-can-i-find-an-element-by-css-class-with-xpath
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByClass =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//*[contains(concat(' ', normalize-space(@class), ' '), ' {target} ')]");


        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByTag =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//{target}");

        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ById =>
            (target, prefixes, driver) => Permutate(prefixes, driver, (prefix) => 
                $"{prefix}//*[@id = {target.XpathEncode()}]");

        #endregion

        #region Prefixes
        virtual protected Func<string, IEnumerable<string>> TableRow =>
            (target) => new List<string>(){
                $"//tr[td[{TextMatch(target)}]]",
                $"//tr[td/*[{MarkerElements} and {TextMatch(target)}]]",
                $"//tr[td/*[@value = {target.XpathEncode()}]]",
                $"//tr[td/select/option[@selected='selected' and {TextMatch(target)}]]"
            };

        virtual protected Func<string, IEnumerable<string>> DivRoleRow =>
            (target) => new List<string>() {
                $"//*[{MarkerElements} and {TextMatch(target)}]/ancestor::div[@role='row'][1]",
                $"//*[@value = {target.XpathEncode()}]/ancestor::div[@role='row'][1]",
                $"//select[option[@selected='selected' and {TextMatch(target)}]]/ancestor::div[@role='row'][1]",
                $"//div[{TextMatch(target)}]/ancestor-or-self::div[@role='row'][1]"
            };

        virtual protected Func<string, IEnumerable<string>> ParrentDiv =>
            (target) => new List<string>() { $"//div[" +
                    $"{TextMatch(target)} or " +
                    $"*[{MarkerElements} and {TextMatch(target)}] or " +
                    $"*[{ActiveElements} and @value = {target.XpathEncode()}] or " +
                    $"@name={target.XpathEncode()} or " +
                    $"@aria-label={target.XpathEncode()}" +
                $"]" };

        virtual protected Func<string, IEnumerable<string>> ParrentDivWithRowRole =>
            (target) => new List<string>() {
                $"//*[{MarkerElements} and {TextMatch(target)}]/ancestor::div[@role='row'][1]",
                $"//*[@value = {target.XpathEncode()}]/ancestor::div[@role='row'][1]",
                $"//select[option[@selected='selected' and {TextMatch(target)}]]/ancestor::div[@role='row'][1]"
            };

        virtual protected Func<string, IEnumerable<string>> FollowingRow =>
            (target) => TableRow(target).Select(x => $"{x}/following-sibling::tr[1]").ToList();
        #endregion

        virtual protected string TextMatch(string target)
            => $"text()[normalize-space(.)={target.XpathEncode()} or normalize-space(.)={$"{target}:".XpathEncode()}]";

        virtual protected string MarkerElements
            => "( self::label or self::b or self::h1 or self::h2 or self::h3 or self::h4 or self::h5 or self::h6 or self::span )";

        virtual protected string ContentElements
            => "( self::label or self::b or self::h1 or self::h2 or self::h3 or self::h4 or self::h5 or self::h6 or self::span " +
            "or self::p or self::div or self::option)";

        virtual protected string ActiveElements
            => "( self::a or self::button or self::input or self::select or self::textarea or @role='button' or @role='link' or @role='menuitem' )";
    }
}
