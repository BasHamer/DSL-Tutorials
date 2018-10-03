using System;
using System.Collections.Generic;
using System.Text;

namespace PossumLabs.Specflow.Core.Files
{
    public class FileManager
    {
        public FileManager(DatetimeManager datetimeManager)
        {
            Start = datetimeManager.Now();
            Index = 0;
        }

        public void Initialize(string featureName, string scenarioName, string example = null)
        {
            FeatureName = featureName;
            ScenarioName = scenarioName;
            ExampleName = example;
        }
        private DateTime Start { get; }
        private int Index { get; }

        private string FeatureName { get; set; }
        private string ScenarioName { get; set; }
        private string ExampleName { get; set; }

        //public Uri GetFileName(string type, string extension)
        //{
        //    if (ExampleName == null)
        //        return $"{FeatureName}-{ScenarioName}-{ExampleName}-{type}.{extension}";
        //    else
        //        return $"";
        //}
    }
}
