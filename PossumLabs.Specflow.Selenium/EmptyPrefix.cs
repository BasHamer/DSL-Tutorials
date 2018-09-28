using System;
using System.Collections.Generic;
using System.Text;

namespace PossumLabs.Specflow.Selenium
{
    public class EmptySelectorPrefix : SelectorPrefix
    {
        public EmptySelectorPrefix()
        {
        }

        public override IEnumerable<string> CreateXpathPrefixes()
            => new string[]{ string.Empty };
    }
}
