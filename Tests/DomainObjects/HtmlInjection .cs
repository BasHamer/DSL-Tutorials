﻿using PossumLabs.Specflow.Core.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyTest.DomainObjects
{
    public class HtmlInjection : IValueObject
    {
        public string Content { get; set; }

        public string Html
        {
            set
            {
                Content = $"<html><head></head><body>{value}</body></html>";
            }
        }

        public string Form
        {
            set
            {
                Content = $"<html><head></head><body><form>{value}</form></body></html>";
            }
        }

        public string TableRow
        {
            set
            {
                Content = $"<html><head></head><body><table><tr>{value}</tr></table></body></html>";
            }
        }
    }
}
