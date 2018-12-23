using Microsoft.Extensions.Configuration;
using PossumLabs.Specflow.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace PossumLabs.Specflow.Selenium.Configuration
{
    public class SeleniumGridConfiguration
    {

        public SeleniumGridConfiguration()
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .Build();
            Url = config["seleniumGridUrl"];
            if(!int.TryParse(config["seleniumRetryMs"], out var retry))
                new GherkinException($"Can't parse seleniumRetryMs, found value {config["seleniumRetryMs"]} this has to be an integer like 10000 for 10 seconds");
            RetryMs = retry;
            Width = int.Parse(config["seleniumWidth"]??"1280");
            Height = int.Parse(config["seleniumHeight"]??"720");
        }

        public string Url { get; }
        public int RetryMs { get; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
    }
}
