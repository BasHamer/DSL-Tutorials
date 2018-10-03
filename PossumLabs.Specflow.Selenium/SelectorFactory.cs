using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PossumLabs.Specflow.Selenium
{
    public class SelectorFactory
    {
        private static readonly Core.EqualityComparer<IWebElement> Comparer =
            new Core.EqualityComparer<IWebElement>((x, y) => x.Location == y.Location && x.TagName == y.TagName);

        public Selector Create(string constructor)
        {
            if (Parser.IsId.IsMatch(constructor))
                return new Selector(constructor, By.Id(Parser.IsId.Match(constructor).Groups[1].Value));
            if (Parser.IsElement.IsMatch(constructor))
                return new Selector(constructor, By.TagName(Parser.IsElement.Match(constructor).Groups[1].Value));
            if (Parser.IsClass.IsMatch(constructor))
                return new Selector(constructor, By.ClassName(Parser.IsClass.Match(constructor).Groups[1].Value));
            return new Selector(constructor, SequencedSelectorsByOrder);
        }

        public RowSelectorPrefix CreateRowPrefix(string constructor)
        {
            if (Parser.IsId.IsMatch(constructor))
                return new RowSelectorPrefix(constructor, $"//*[id={Parser.IsId.Match(constructor).Groups[1].Value.XpathEncode()}]");
            if (Parser.IsElement.IsMatch(constructor))
                return new RowSelectorPrefix(constructor, $"//{Parser.IsElement.Match(constructor).Groups[1].Value}");
            if (Parser.IsClass.IsMatch(constructor))
                return new RowSelectorPrefix(constructor, $"//*[class={Parser.IsClass.Match(constructor).Groups[1].Value.XpathEncode()}]");
            return new RowSelectorPrefix(constructor, SequencedRowPrefixesByOrder);
        }

        public UnderSelectorPrefix CreateUnderPrefix(string constructor)
        {
            if (Parser.IsId.IsMatch(constructor))
                return new UnderSelectorPrefix(constructor, $"//*[id={Parser.IsId.Match(constructor).Groups[1].Value.XpathEncode()}]");
            if (Parser.IsElement.IsMatch(constructor))
                return new UnderSelectorPrefix(constructor, $"//{Parser.IsElement.Match(constructor).Groups[1].Value}");
            if (Parser.IsClass.IsMatch(constructor))
                return new UnderSelectorPrefix(constructor, $"//*[class={Parser.IsClass.Match(constructor).Groups[1].Value.XpathEncode()}]");
            return new UnderSelectorPrefix(constructor, SequencedUnderPrefixesByOrder);
        }

        virtual protected List<Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>>> SequencedSelectorsByOrder
           => new List<Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>>>
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
           };

        virtual protected List<Func<string, IEnumerable<string>>> SequencedRowPrefixesByOrder
           => new List<Func<string, IEnumerable<string>>>
           {
               TableRow,
               DivRoleRow,
           };

        virtual protected List<Func<string, IEnumerable<string>>> SequencedUnderPrefixesByOrder
           => new List<Func<string, IEnumerable<string>>>
           {
                ParrentDiv,
                ParrentDivWithRowRole,
                FollowingRow
           };

        private bool Filter(IWebElement e) =>
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
            (target, prefixes, driver) => 
                prefixes.CrossMultiply().Select(prefix=>
                    driver
                    .FindElements(By.XPath($"{prefix}//label[{TextMatch(target)}]/*[{ActiveElements}]"))
                    .Where(Filter)
                    .Distinct(Comparer)
                    .Select(e => CreateElement(driver, e))
                ).FirstOrDefault(e=>e.Any()) ?? new Element[] { };

        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> SpecialButtons =>
            (target, prefixes, driver) =>
                prefixes.CrossMultiply().Select(prefix => 
                    driver
                    .FindElements(By.XPath($"{prefix}//*[(self::input or self::button) and @type={target.XpathEncode()} and (@type='submit' or @type='reset')]"))
                    .Where(Filter)
                    .Distinct(Comparer)
                    .Select(e => CreateElement(driver, e))
                ).FirstOrDefault(e => e.Any()) ?? new Element[] { };

        //<input aria-label="target">
        //*[(self::a or self::button or @role='button' or @role='link' or @role='menuitem' or self::input or self::textarea or self::select) and @aria-label='{target}']
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByNested =>
            (target, prefixes, driver) =>
                prefixes.CrossMultiply().Select(prefix => 
                    driver
                        .FindElements(By.XPath(
                            $"{prefix}//*[{ActiveElements} and (" +
                                $"{TextMatch(target)} or " +
                                $"label[{TextMatch(target)}] or " +
                                $"((@type='button' or @type='submit' or @type='reset') and @value={target.XpathEncode()}) or " +
                                $"@name={target.XpathEncode()} or " +
                                $"@aria-label={target.XpathEncode()} or " +
                                $"(@type='radio' and @value={target.XpathEncode()})" +
                            $")]"))
                        .Where(Filter)
                        .Distinct(Comparer)
                        .Select(e => CreateElement(driver, e))
                ).FirstOrDefault(e => e.Any()) ?? new Element[] { };

        //<a href = "https://www.w3schools.com/html/" >target</a>
        //*[(self::a or self::button or @role='button' or @role='link' or @role='menuitem') and text()='{target}']
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByText =>
            (target, prefixes, driver) =>
                prefixes.CrossMultiply().Select(prefix => 
                    driver
                        .FindElements(By.XPath($"{prefix}//*[{ActiveElements} and {TextMatch(target)}]"))
                        .Where(Filter)
                        .Distinct(Comparer)
                        .Select(e => CreateElement(driver, e))
            ).FirstOrDefault(e => e.Any()) ?? new Element[] { };

        //<a href = "https://www.w3schools.com/html/" title="target">Visit our HTML Tutorial</a>
        //a[@title='{target}']
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByTitle =>
            (target, prefixes, driver) =>
                prefixes.CrossMultiply().Select(prefix => 
                    driver
                        .FindElements(By.XPath($"{prefix}//a[@title={target.XpathEncode()}]"))
                        .Where(Filter)
                        .Distinct(Comparer)
                        .Select(e => new Element(e, driver))
            ).FirstOrDefault(e => e.Any()) ?? new Element[] { };

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

        //<a href = "https://www.w3schools.com/html/" title="target">Visit our HTML Tutorial</a>
        //a[@title='{target}']
        virtual protected Func<string, IEnumerable<SelectorPrefix>, IWebDriver, IEnumerable<Element>> ByFollowingMarker =>
            (target, prefixes, driver) =>
                prefixes.CrossMultiply().Select(prefix =>
                    driver
                        .FindElements(By.XPath($"{prefix}//*[{MarkerElements} and {TextMatch(target)}]/following-sibling::*[not(self::br or self::hr)][1][{ActiveElements}]"))
                        .Where(Filter)
                        .Distinct(Comparer)
                        .Select(e => new Element(e, driver))
            ).FirstOrDefault(e => e.Any()) ?? new Element[] { };
        #endregion

        #region Prefixes
        virtual protected Func<string, IEnumerable<string>> TableRow =>
            (target) => new List<string>(){
                $"//tr[td[normalize-space() = {target.XpathEncode()}]]",
                $"//tr[td/*[{MarkerElements} and {TextMatch(target)}]]",
                $"//tr[td/*[@value = {target.XpathEncode()}]]",
                $"//tr[td/select/option[@selected='selected' and {TextMatch(target)}]]"
            };

        virtual protected Func<string, IEnumerable<string>> DivRoleRow =>
            (target) => new List<string>() {
                $"//*[{MarkerElements} and normalize-space() = {target.XpathEncode()}]/ancestor::div[@role='row'][1]",
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
            => $"text()[normalize-space(.)={target.XpathEncode()}]";

        virtual protected string MarkerElements 
            => "( self::label or self::b or self::h1 or self::h2 or self::h3 or self::h4 or self::h5 or self::h6 or self::span )";

        virtual protected string ActiveElements 
            => "( self::a or self::button or self::input or self::select or self::textarea or @role='button' or @role='link' or @role='menuitem' )";
    }
}
