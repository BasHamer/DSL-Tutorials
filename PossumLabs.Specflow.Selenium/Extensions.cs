﻿using System;
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

        public static IEnumerable<string> CrossMultiply(this IEnumerable<SelectorPrefix> prefixes)
        {
            var prefixOptions = prefixes.Select(x => x.CreateXpathPrefixes().ToList()).ToList();
            var options = AllCombinationsOf(prefixOptions).Select(o => o.Aggregate((x, y) => x + y));
            return options;
        }

        public static List<List<T>> AllCombinationsOf<T>(List<List<T>> sets)
        {
            // need array bounds checking etc for production
            var combinations = new List<List<T>>();

            // prime the data
            foreach (var value in sets[0])
                combinations.Add(new List<T> { value });

            foreach (var set in sets.Skip(1))
                combinations = AddExtraSet(combinations, set);

            return combinations;
        }

        private static List<List<T>> AddExtraSet<T>
             (List<List<T>> combinations, List<T> set)
        {
            var newCombinations = from value in set
                                  from combination in combinations
                                  select new List<T>(combination) { value };

            return newCombinations.ToList();
        }
    }
}
