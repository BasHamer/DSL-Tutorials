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
                LooseFollowingRow
            }).ToList();

        protected Func<string, IEnumerable<string>> LooseFollowingRow =>
            (target) => TableRow(target).Select(x => $"{x}/ancestor::tr/following-sibling::tr[1]").ToList();

    }
}
