using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.Reflection;

namespace RD.RandomNameGenService.Models
{
    public class RandomPersonData
    {
        Random rand = null;

        public RandomPersonData()
        {
            int seconds = DateTime.Now.Second;
            rand = new Random(seconds);
        }

        RDNamesDatabase pool;
        int index;
        IEnumerable<int> sizes;
        int total;
        int random;
        int pointer;
        string gender;
        Boolean found;
        List<RDNamesDatabase> matches;
        int male_size;
        int name_index;
        string name;
        int name_chunk_count;
        int surname_index;
        string surname;
        string subject;
        IList<IList<string>> gender_exceptions;
        object birthYear;
        DateTime randDate;
        int age;
        string femaleTitle;
        string[] separation;
        object email;
        string[] signs;
        object photos;
        IEnumerable<string> gender_patterns;
        IEnumerable<string> gender_replacements;
        IEnumerable<string> general_patterns;
        IEnumerable<string> general_replacements;
        List<string> chunks;
        List<string> name_chunks;
        PersonDataModel result;

        const string ANY = null;

        private static bool property_exists(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName) != null;
        }

        IEnumerable<int> get_sizes(List<RDNamesDatabase> database)
        {
            return database.Select(p => (p.male.Count() + p.female.Count()) * p.surnames.Count());
        }

        RDNamesDatabase get_random_pool(List<RDNamesDatabase> database)
        {
            index = rand.Next(0, database.Count - 1);
            pool = database[index];
            return pool;
        }

        RDNamesDatabase get_random_pool_by_size(List<RDNamesDatabase> database)
        {
            sizes = get_sizes(database);
            total = ((IEnumerable<int>)sizes).Sum();
            random = rand.Next(0, total);
            pointer = 0;

            foreach (var size in sizes)
            {
                index = size;
                pointer += size;
                if (pointer >= random)
                    break;
            }

            pool = database[index];
            return pool;
        }

        public object GetNames(RandomPersonDataSettings randomPersonDataSettings)
        {
            if (randomPersonDataSettings == null)
                throw new Exception("Random Person Data Settings cannot be empty");

            return RandomNamesSend(randomPersonDataSettings.database, randomPersonDataSettings.region, randomPersonDataSettings.language, randomPersonDataSettings.gender, randomPersonDataSettings.amount, randomPersonDataSettings.minlen, randomPersonDataSettings.maxlen = 5000, randomPersonDataSettings.extData);
        }

        private object RandomNamesSend(List<RDNamesDatabase> database, string region = ANY, string language = ANY, string gender = ANY, int amount = 1, int minlen = 10, int maxlen = 5000, bool IsIncludeExtData = false)
        {
            List<PersonDataModel> results = new List<PersonDataModel>();

            try
            {
                if (amount < 1 || amount > 1000)
                {
                    throw new Exception("Amount of requested names exceeds maximum allowed");
                }

                int count = 0;
                while (count < amount)
                {
                    PersonDataModel name = generate_name(database, region, language, gender, IsIncludeExtData);
                    int name_length = (name["name"] + " " + name["surname"]).Length;

                    if (name_length >= minlen && name_length <= maxlen)
                    {
                        results.Add(name);
                        count++;
                    }
                }

                if (amount == 1)
                {
                    return results[0];
                }
                else
                {
                    return results;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private PersonDataModel generate_name(List<RDNamesDatabase> database, string region = ANY, string language = ANY, string gender = ANY, bool IsIncludeExtData = false)
        {
            if (region == ANY || region == "random")
            {
                pool = get_random_pool(database);
            }
            else
            {
                // find pool by region name and language
                found = false;
                matches = new List<RDNamesDatabase>();

                foreach (var pool in database)
                {
                    if (pool.region.ToLower() == region.ToLower())
                        matches.Add(pool);
                }

                if (language == ANY && matches.Count > 0)
                {
                    pool = get_random_pool(matches);
                    found = true;
                }
                //else if (pool.language != null)
                //{
                //    foreach (var match in matches)
                //    {
                //        if (match.language.ToLower() != language.ToLower())
                //            continue;
                //        found = true;
                //        pool = match;
                //    }
                //}

                if (!found) throw new Exception("Region or language not found");
            }

            if (gender == ANY || gender == "random")
            {
                // we"re being sexist here to make the code more effective
                male_size = pool.male.Count;
                total = male_size + pool.female.Count;
                gender = (rand.Next(0, total) >= male_size) ? "female" : "male";
            }
            else if (gender != "male" && gender != "female")
            {
                // transphobic now too
                throw new Exception("Invalid gender");
            }

            // find random name and apply exceptions
            name_index = rand.Next(0, ((List<string>)(pool[gender])).Count - 1);
            name = ((List<string>)(pool[gender]))[name_index];
            name_chunk_count = Regex.Matches(name, " ").Count;//substr_count(name, " ");
            // find random surname and apply exceptions
            surname_index = rand.Next(0, pool.surnames.Count - 1);
            surname = pool.surnames[surname_index];
            subject = name + " " + surname;
            // do magic exception stuff, don"t ever ask about this
            gender_exceptions = (IList<IList<string>>)pool[gender + "_exceptions"];
            gender_patterns = gender_exceptions != null ? gender_exceptions.Select(p => p[0]) : new List<string>();
            gender_replacements = gender_exceptions != null ? gender_exceptions.Select(p => p[1]) : new List<string>();

            Regex preg_replace = null;

            if (gender_patterns.Count() > 0)
            {
                preg_replace = new Regex(gender_patterns.First(), RegexOptions.IgnoreCase);
                subject = preg_replace.Replace(subject, gender_replacements.First());
                //subject			 = preg_replace(gender_patterns, gender_replacements, subject);
            }

            general_patterns = pool.exceptions != null ? pool.exceptions[0] : new List<string>();
            general_replacements = pool.exceptions != null ? pool.exceptions[1] : new List<string>();

            if (general_patterns.Count() > 0)
            {
                preg_replace = new Regex(general_patterns.First(), RegexOptions.IgnoreCase);
                subject = preg_replace.Replace(subject, general_replacements.First());
            }

            //subject			  = preg_replace(general_patterns, general_replacements, subject);
            // this works 99.7% of the time, maybe
            chunks = subject.Split(' ').ToList(); //explode(" ", subject);
            name_chunks = chunks.Splice(0, name_chunk_count == 0 ? 1 : name_chunk_count - 1).ToList();
            chunks.Remove(name_chunks[0]);//array_splice(chunks, 0, name_chunk_count - 1);
            name = string.Join(" ", name_chunks); //implode(" ", name_chunks);
            surname = string.Join(" ", chunks); //implode(" ", chunks);

            result = new PersonDataModel()
            {
                name = name,
                surname = surname,
                gender = gender,
                region = pool.region
            };

            if (!string.IsNullOrEmpty(pool.language))
            {
                result.language = pool.language;
            }

            // return data
            return IsIncludeExtData ? fn_generate_ext_result(result) : result;
        }

        private PersonDataModel fn_generate_ext_result(PersonDataModel result)
        {
            name = result.name;
            surname = result.surname;
            gender = result.gender;

            randDate = rand.Next(RelativeDateParser.Parse("-36 years"), RelativeDateParser.Parse("-21 years"));

            birthYear = randDate.Year;

            //age
            age = (DateTime.Now.Year - randDate.Year);

            result.age = age;
            //title
            femaleTitle = (age > 26 && rand.Next(0, 100) > 65) ? "mrs" : "ms";
            result.title = (gender == "male") ? "mr" : femaleTitle;
            //phone
            result.phone = "(" + rand.Next(100, 999) + ") " + rand.Next(100, 999) + " " + rand.Next(1000, 9999);
            //birthday
            result.birthday = new Birthday();
            result.birthday.dmy = randDate.ToString("dd/MM/yyyy");
            result.birthday.mdy = randDate.ToString("MM/dd/yyyy");
            result.birthday.raw = Convert.ToInt32(DateTimeToUnixTimestamp(randDate));
            //email address
            separation = new string[] { ".", "_", "-", "" };

            if ((name + surname).Length >= 12)
            {
                email = (name).ToLower() + separation[rand.Next(1, 3)] + birthYear;
            }
            else
            {
                email = ((name).ToLower() + separation[rand.Next(0, 3)] + surname);
            }

            result.email = "" + email + "@example.com";
            //password
            signs = new string[] { "!", "@", "#", "", "%", "^", "&", "*", "(", ")", "{", "}", "~", "+", "=", "_", "" };
            result.password = (surname.Replace(" ", "") + birthYear + signs[rand.Next(0, (signs).Count() - 1)] + signs[rand.Next(0, (signs).Count() - 1)]);
            //credit card
            result.credit_card = new CreditCard();
            result.credit_card.expiration = rand.Next(1, 12) + "/" + (DateTime.Now.Year + rand.Next(1, 8));
            result.credit_card.number = rand.Next(1000, 9999) + "-" + rand.Next(1000, 9999) + "-" + rand.Next(1000, 9999) + "-" + rand.Next(1000, 9999);
            result.credit_card.pin = rand.Next(1000, 9999);
            result.credit_card.security = rand.Next(100, 999);
            //photo
            photos = (gender == "male") ? ("photos/male/*.jpg") : ("photos/female/*.jpg");
            return result;
        }

        private static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (TimeZoneInfo.ConvertTimeToUtc(dateTime) - new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
        }
    }

    public class RandomPersonDataSettings
    {
        string ANY = string.Empty;
        public List<RDNamesDatabase> database { get; set; }
        public string region { get; set; }
        public string language { get; set; }
        public string gender { get; set; }
        public int amount { get; set; }
        public int minlen { get; set; }
        public int maxlen { get; set; }
        public bool extData { get; set; }

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

        public RandomPersonDataSettings()
        {
            database = new List<RDNamesDatabase>();
            region = ANY;
            language = ANY;
            gender = ANY;
            amount = 1;
            minlen = 10;
            maxlen = 5000;
            extData = false;
        }

        public RandomPersonDataSettings(string parameters)
        {
            try
            {
                string[] param = parameters.Split(',');

                foreach (string par in param)
                {
                    string[] keyVal = par.Split('=');
                    if (this.GetType().HasProperty(keyVal[0]))
                    {
                        PropertyInfo prop = this.GetType().GetProperty(keyVal[0]);
                        prop.SetValue(this, GetObjectOfTypeFromString(prop.PropertyType, keyVal[1]), null);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public object GetObjectOfTypeFromString(Type propType , string Value)
        {
            //Type propType = typeof(T);
            object returnValue = null;

            if (!string.IsNullOrEmpty(Value))
            {
                returnValue = ChangeType(Value, propType);
            }

            return returnValue;
        }

        private object ChangeType(object value, Type conversion)
        {
            var t = conversion;

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return Convert.ChangeType(value, t);
        }
    }

    public static class Extensions
    {
        public static IEnumerable<T> Splice<T>(this IEnumerable<T> list, int offset, int count)
        {
            return list.Skip(offset).Take(count);
        }

        public static DateTime Next(this Random Rand, DateTime minValue, DateTime maxValue)
        {
            Random gen = new Random();
            int range = (maxValue - minValue).Days;
            return minValue.AddDays(gen.Next(range));
        }

        public static bool HasProperty(this Type obj, string propertyName)
        {
            return obj.GetProperty(propertyName) != null;
        }
        
        public static PropertyInfo GetProperty(this Type obj, string propertyName)
        {
            return obj.GetProperty(propertyName);
        }

    }

    public class RelativeDateParser
    {
        private const string ValidUnits = "year|month|week|day|hour|minute|second";

        /// <summary>
        /// Ex: "last year"
        /// </summary>
        private static readonly Regex _basicRelativeRegex = new Regex(@"^(last|next) +(" + ValidUnits + ")$");

        /// <summary>
        /// Ex: "+1 week"
        /// Ex: " 1week"
        /// </summary>
        private static readonly Regex _simpleRelativeRegex = new Regex(@"^([+-]?\d+) *(" + ValidUnits + ")s?$");

        /// <summary>
        /// Ex: "2 minutes"
        /// Ex: "3 months 5 days 1 hour ago"
        /// </summary>
        private static readonly Regex _completeRelativeRegex = new Regex(@"^(?: *(\d) *(" + ValidUnits + ")s?)+( +ago)?$");

        public static DateTime Parse(string input)
        {
            // Remove the case and trim spaces.
            input = input.Trim().ToLower();

            // Try common simple words like "yesterday".
            var result = TryParseCommonDateTime(input);
            if (result.HasValue)
                return result.Value;

            // Try common simple words like "last week".
            result = TryParseLastOrNextCommonDateTime(input);
            if (result.HasValue)
                return result.Value;

            // Try simple format like "+1 week".
            result = TryParseSimpleRelativeDateTime(input);
            if (result.HasValue)
                return result.Value;

            // Try first the full format like "1 day 2 hours 10 minutes ago".
            result = TryParseCompleteRelativeDateTime(input);
            if (result.HasValue)
                return result.Value;

            // Try parse fixed dates like "01/01/2000".
            return DateTime.Parse(input);
        }

        private static DateTime? TryParseCommonDateTime(string input)
        {
            switch (input)
            {
                case "now":
                    return DateTime.Now;
                case "today":
                    return DateTime.Today;
                case "tomorrow":
                    return DateTime.Today.AddDays(1);
                case "yesterday":
                    return DateTime.Today.AddDays(-1);
                default:
                    return null;
            }
        }

        private static DateTime? TryParseLastOrNextCommonDateTime(string input)
        {
            var match = _basicRelativeRegex.Match(input);
            if (!match.Success)
                return null;

            var unit = match.Groups[2].Value;
            var sign = string.Compare(match.Groups[1].Value, "next", true) == 0 ? 1 : -1;
            return AddOffset(unit, sign);
        }

        private static DateTime? TryParseSimpleRelativeDateTime(string input)
        {
            var match = _simpleRelativeRegex.Match(input);
            if (!match.Success)
                return null;

            var delta = Convert.ToInt32(match.Groups[1].Value);
            var unit = match.Groups[2].Value;
            return AddOffset(unit, delta);
        }

        private static DateTime? TryParseCompleteRelativeDateTime(string input)
        {
            var match = _completeRelativeRegex.Match(input);
            if (!match.Success)
                return null;

            var values = match.Groups[1].Captures;
            var units = match.Groups[2].Captures;
            var sign = match.Groups[3].Success ? -1 : 1;
            //Debug.Assert(values.Count == units.Count);

            var dateTime = UnitIncludeTime(units) ? DateTime.Now : DateTime.Today;

            for (int i = 0; i < values.Count; ++i)
            {
                var value = sign * Convert.ToInt32(values[i].Value);
                var unit = units[i].Value;

                dateTime = AddOffset(unit, value, dateTime);
            }

            return dateTime;
        }

        /// <summary>
        /// Add/Remove years/days/hours... to a datetime.
        /// </summary>
        /// <param name="unit">Must be one of ValidUnits</param>
        /// <param name="value">Value in given unit to add to the datetime</param>
        /// <param name="dateTime">Relative datetime</param>
        /// <returns>Relative datetime</returns>
        private static DateTime AddOffset(string unit, int value, DateTime dateTime)
        {
            switch (unit)
            {
                case "year":
                    return dateTime.AddYears(value);
                case "month":
                    return dateTime.AddMonths(value);
                case "week":
                    return dateTime.AddDays(value * 7);
                case "day":
                    return dateTime.AddDays(value);
                case "hour":
                    return dateTime.AddHours(value);
                case "minute":
                    return dateTime.AddMinutes(value);
                case "second":
                    return dateTime.AddSeconds(value);
                default:
                    throw new Exception("Internal error: Unhandled relative date/time case.");
            }
        }

        /// <summary>
        /// Add/Remove years/days/hours... relative to today or now.
        /// </summary>
        /// <param name="unit">Must be one of ValidUnits</param>
        /// <param name="value">Value in given unit to add to the datetime</param>
        /// <returns>Relative datetime</returns>
        private static DateTime AddOffset(string unit, int value)
        {
            var now = UnitIncludesTime(unit) ? DateTime.Now : DateTime.Today;
            return AddOffset(unit, value, now);
        }

        private static bool UnitIncludeTime(CaptureCollection units)
        {
            foreach (Capture unit in units)
                if (UnitIncludesTime(unit.Value))
                    return true;
            return false;
        }

        private static bool UnitIncludesTime(string unit)
        {
            switch (unit)
            {
                case "hour":
                case "minute":
                case "second":
                    return true;

                default:
                    return false;
            }
        }
    }
}