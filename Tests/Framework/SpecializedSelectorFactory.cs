using OpenQA.Selenium;
using PossumLabs.Specflow.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyTest.Framework
{
    public class SpecializedSelectorFactory : SelectorFactory
    {
        override protected List<Func<string, IEnumerable<string>>> SequencedUnderPrefixesByOrder
            => base.SequencedUnderPrefixesByOrder.Concat(new List<Func<string, IEnumerable<string>>>
            {
                LooseFollowingRow,
                ParrentRowTableLayout
            }).ToList();


        override protected Element CreateElement(IWebDriver driver, IWebElement e)
        {
            if (e.TagName == "select" || (e.TagName == "input" && !string.IsNullOrEmpty(e.GetAttribute("list"))))
                return new SloppySelectElement(e, driver);
            return base.CreateElement(driver, e);
        }

        protected Func<string, IEnumerable<string>> LooseFollowingRow =>
            (target) => TableRow(target).Select(x => $"{x}/ancestor::tr/following-sibling::tr[1]").ToList();

        virtual protected Func<string, IEnumerable<string>> ParrentRowTableLayout =>
            (target) => new List<string>() {
                $"//*[{MarkerElements} and {TextMatch(target)}]/ancestor::td[1]",
        };

    }
}
