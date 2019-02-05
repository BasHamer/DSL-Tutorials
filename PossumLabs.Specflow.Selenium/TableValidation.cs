using PossumLabs.Specflow.Core;
using PossumLabs.Specflow.Core.Validations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PossumLabs.Specflow.Selenium
{
    public class TableValidation : Validation
    {
        public TableValidation(List<Dictionary<string, WebValidation>> validations) : base((o)=>AggregatePredicate(o,validations), "")
        {
            Header = validations.First().Keys.ToList();
        }

        public List<string> Header { get; }

        private static string AggregatePredicate(object o, List<Dictionary<string, WebValidation>> validations)
        {
            if (!(o is TableElement))
                return "This validation can only work on Tables";

            var table = o as TableElement;

            foreach(var rowValidation in validations)
            {
                var rowId = table.GetRowId(rowValidation.First().Value.Text, rowValidation.First().Key);

                foreach( var column in rowValidation.Skip(1))
                {
                    var elements = table.GetContentElement(rowId, column.Key);
                    var results = elements.Select(e => column.Value.Predicate(e));
                    if (results.Any(result=>result == null))
                        return null;
                    return results.Distinct().LogFormat();
                }
            }

            return null;
        }
    }
}
