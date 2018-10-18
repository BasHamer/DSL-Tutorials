using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PossumLabs.Specflow.Selenium
{
    public class UnderSelectorPrefix : SelectorPrefix
    {
        public UnderSelectorPrefix(string constructor, string xpathPrefix)
        {
            this.Constructor = constructor;
            this.XpathPrefix = xpathPrefix;
        }

        public UnderSelectorPrefix(string constructor, List<Func<string, IEnumerable<string>>> sequencedRowPrefixesByOrder)
        {
            this.Constructor = constructor;
            this.SequencedRowPrefixesByOrder = sequencedRowPrefixesByOrder;
        }

        private string Constructor { get; }
        private string XpathPrefix { get; }
        private List<Func<string, IEnumerable<string>>> SequencedRowPrefixesByOrder { get; }

        public override IEnumerable<string> CreateXpathPrefixes()
            => SequencedRowPrefixesByOrder != null ? SequencedRowPrefixesByOrder.SelectMany(f => f(Constructor)) : new string[] { XpathPrefix };
    }
}
