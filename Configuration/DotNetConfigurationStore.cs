using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;

namespace Configuration
{
    /// <summary>Provides simplified access to a .Net configuration file.</summary>
    public class DotNetConfigurationStore : IConfigurationStore
    {
        /// <summary>Gets or sets the configuration files name.</summary>
        public string FileName { get; set; }

        /// <summary>Retrieves all available values from the specified section.</summary>
        /// <param name="name">Name of the section.</param>
        /// <returns>A dictionary with all keys and values.</returns>
        public IDictionary<string, object> GetValues(string name)
        {
            var section = GetSection(name);

            return (from child
                    in section.Children.OfType<KeyValueConfigurationElement>()
                    select child)
                    .ToDictionary(e => e.Key, e => (object) e.Value);
        }

        /// <summary>Stores the provided values in a configuration file. </summary>
        /// <param name="name">Used as the sections name.</param>
        /// <param name="values">The keys and values which will be stored.</param>
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


        private KeyValueConfigurationSection GetSection(string name)
        {
            // TODO: Create a default configuration including file and boiler plate content

            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(name);
            if (!File.Exists(FileName)) throw new FileNotFoundException("", FileName);

            var filemap = new ExeConfigurationFileMap {ExeConfigFilename = FileName};
            var configuration = ConfigurationManager.OpenMappedExeConfiguration(filemap, ConfigurationUserLevel.None);
            var section = configuration.GetSection(name);

            if (section != null && 
                !(section is KeyValueConfigurationSection))
            {
                throw new NotSupportedException($"Type {section.GetType().Name} is not supported.");
            }

            // If the section does not exist we will simply create it
            if (section == null)
            {
                section = new KeyValueConfigurationSection();
                configuration.Sections.Add(name, section);
            }

            return (KeyValueConfigurationSection)section;
        }
    }
}