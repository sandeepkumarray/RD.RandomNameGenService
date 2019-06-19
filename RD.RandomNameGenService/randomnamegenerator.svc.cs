using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using RD.RandomNameGenService.Models;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace RD.RandomNameGenService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "randomnamegenerator" in code, svc and config file together.
    public class randomnamegenerator : Irandomnamegenerator
    {
        public string generate_names(string parameters)
        {
            try
            {
                List<RDNamesDatabase> RDNamesDatabaseList = new List<RDNamesDatabase>();
                string jsonFilePath = @"C:\Sandeep R\TestProjects\names.db";
                string excelInJson = File.ReadAllText(jsonFilePath);
                if (!string.IsNullOrEmpty(excelInJson))
                {
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ContractResolver = new NullToEmptyStringResolver()
                    };

                    RDNamesDatabaseList = JsonConvert.DeserializeObject<List<RDNamesDatabase>>(excelInJson, settings);
                }

                RandomPersonData uiname = new RandomPersonData();
                RandomPersonDataSettings randomPersonDataSettings = new RandomPersonDataSettings(parameters);
                randomPersonDataSettings.database = RDNamesDatabaseList;
                object UINamesModelobj = uiname.GetNames(randomPersonDataSettings);

                return JsonConvert.SerializeObject(UINamesModelobj);
            }
            catch(Exception e)
            {
                return JsonConvert.SerializeObject(e);
            }
        }
    }


    public class NullToEmptyStringResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            return type.GetProperties()
                    .Select(p =>
                    {
                        var jp = base.CreateProperty(p, memberSerialization);
                        jp.ValueProvider = new EmptyStringToNullValueProvider(p);
                        return jp;
                    }).ToList();
        }
    }

    public class EmptyStringToNullValueProvider : IValueProvider
    {
        PropertyInfo _MemberInfo;
        public EmptyStringToNullValueProvider(PropertyInfo memberInfo)
        {
            _MemberInfo = memberInfo;
        }

        public object GetValue(object target)
        {
            object result = _MemberInfo.GetValue(target, null);
            if (_MemberInfo.PropertyType == typeof(string) && Convert.ToString(result) == "")
                result = null;
            return result;

        }

        public void SetValue(object target, object value)
        {
            if (Convert.ToString(value) == "")
                value = null;

            _MemberInfo.SetValue(target, value, null);
        }
    }

}
