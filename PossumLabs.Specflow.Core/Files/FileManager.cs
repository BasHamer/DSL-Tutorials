using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PossumLabs.Specflow.Core.Files
{
    public class FileManager
    {
        public FileManager(DatetimeManager datetimeManager)
        {
            Start = datetimeManager.Now();
            Index = 0;
            IConfiguration config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            BaseFolder = new DirectoryInfo(config["logFolder"]);

            if(!BaseFolder.Exists)
                BaseFolder.Create();
            Order = 1;
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
        private DirectoryInfo BaseFolder { get; }

        public object GetPath(IFile file)
        {
            throw new NotImplementedException();
        }

        private int Order { get; set; }

        private string GetFileName(string type, string extension)
            => $"{FeatureName}-{ScenarioName}-{ExampleName}-{Start.ToString("yyyyMMdd_HHmmss")}-{Order++}-{type}.{extension}";

        public Uri Persist(IFile file)
        {
            Uri path = null;
            using (var fileStream = File.Create(path.ToString()))
            {
                file.Stream.Seek(0, SeekOrigin.Begin);
                file.Stream.CopyTo(fileStream);
            }
            return path;
        }

        public Uri CreateFile(byte[] file, string type, string extention)
        {
            var name = GetFileName(type, extention);
            var info = new FileInfo(Path.Combine(BaseFolder.FullName, name));
            var w = info.Create();
            w.WriteAsync(file, 0, file.Length)
                .ContinueWith((x)=>w.Close());
            return new Uri(info.FullName);
        }

        public Uri CreateFile(string file, string type, string extention)
            => CreateFile(File.ReadAllBytes(file), type, extention);
    }
}
