using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PossumLabs.Specflow.Core;

namespace PossumLabs.Specflow.Selenium
{
    public static class Extensions
    {
        public static string LogFormat(this Dictionary<string, WebValidation> validations)
            => validations.Keys.Select(column => $"column:'{column}' with validation:'{validations[column].Text}'").LogFormat();

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T item)
            =>source.Concat(new List<T> { item });
    }
}
