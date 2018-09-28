using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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
        }

         public string Url { get; }
    }
}
