using System;
using System.Collections.Generic;
using System.Text;

namespace PossumLabs.Specflow.Selenium
{
    public abstract class SelectorPrefix
    {
        public abstract IEnumerable<string> CreateXpathPrefixes();
    }
}
