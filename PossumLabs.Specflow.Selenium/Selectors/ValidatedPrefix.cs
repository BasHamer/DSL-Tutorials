using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PossumLabs.Specflow.Selenium.Selectors
{
    public class ValidatedPrefix : SelectorPrefix
    {
        public ValidatedPrefix()
        {
        }

        private IEnumerable<string> Xpaths {get;set;}
        internal void Init(string v, IEnumerable<string> valid)
            => Xpaths = valid;

        public override IEnumerable<string> CreateXpathPrefixes()
            => Xpaths;
    }
}
