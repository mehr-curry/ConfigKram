using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Configuration
{
    public class DotNetConfigurationStore : IConfigurationStore
    {
        public string FileName { get; set; }

        public IDictionary<string, object> GetValues(string name)
        {
            var section = GetSection(name);

            return (from child
                    in section.Children.OfType<KeyValueConfigurationElement>()
                    select child)
                    .ToDictionary(e => e.Key, e => (object) e.Value);
        }

        public void SetValues(string name, IDictionary<string, object> values)
        {
            var section = GetSection(name);

            foreach (var pair in values)
            {
                var element = section.Children[pair.Key];
                if (element == null)
                {
                    element = new KeyValueConfigurationElement(pair.Key, null);
                    section.Children.Add(element);
                }

                element.Value = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
            }
            
            section.CurrentConfiguration.Save();
        }


        private KeyValueConfigurationSection GetSection(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(groupName);
            if (!File.Exists(FileName)) throw new FileNotFoundException("", FileName);

            var filemap = new ExeConfigurationFileMap {ExeConfigFilename = FileName};
            var configuration = ConfigurationManager.OpenMappedExeConfiguration(filemap, ConfigurationUserLevel.None);
            var section = configuration.GetSection(groupName);

            // gibt es die Section noch nicht, erstellen wir eine neue.
            if (section == null)
            {
                section = new KeyValueConfigurationSection();
                configuration.Sections.Add(groupName, section);
            }

            var keyValueSection = section as KeyValueConfigurationSection;

            return keyValueSection;
        }
    }
}