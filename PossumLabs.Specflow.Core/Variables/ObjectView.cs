using System;
using System.Collections.Generic;
using System.Text;

namespace PossumLabs.Specflow.Core.Variables
{
    public class ObjectView
    {
        public string Var { get; set; }
        public string LogFormat { get; set; }

        public List<string> Values { get; set; }
    }
}
