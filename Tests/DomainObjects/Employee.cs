using PossumLabs.Specflow.Core.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyTest.DomainObjects
{
    public class Employee : IDomainObject
    {
        public Employee()
        {
            Reports = new List<Employee>();
        }
        public string Name { get; set; }
        public List<Employee> Reports { get; set; }
        public string Role { get; set; }
        public int Seniority { get; set; }
        public string LogFormat()
            => Name;
    }
}
