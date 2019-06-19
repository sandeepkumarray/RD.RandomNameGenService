using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;

namespace RD.RandomNameGenService.Models
{
    public class RDNamesDatabase
    {
        public string region { get; set; }
        public string language { get; set; }
        public IList<string> male { get; set; }
        public IList<string> female { get; set; }
        public IList<string> surnames { get; set; }
        public IList<IList<string>> exceptions { get; set; }
        public IList<IList<string>> male_exceptions { get; set; }
        public IList<IList<string>> female_exceptions { get; set; }

        public object this[string propertyName]
        {
            get
            {
                PropertyInfo property = GetType().GetProperty(propertyName);
                return property.GetValue(this, null);
            }
            set
            {
                PropertyInfo property = GetType().GetProperty(propertyName);
                property.SetValue(this, value, null);
            }
        }
    }
}