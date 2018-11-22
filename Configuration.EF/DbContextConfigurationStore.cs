using System;
using System.Collections.Generic;
using System.Linq;

namespace Configuration.EF
{
    /// <summary>Implements a IConfigurationStore for EntityFramework.Core 2 DbContext.</summary>
    public class DbContextConfigurationStore : IConfigurationStore
    {
        /// <summary>Gets the underlying DbContext.</summary>
        public IConfigurationDbContext Context { get; }

        /// <summary>Create a new instance for the passed DbContext</summary>
        /// <param name="context">Cannot be null or empty</param>
        public DbContextConfigurationStore(IConfigurationDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>Returns the configuration entries for one specific type of configuration</summary>
        /// <param name="name">Name of the requested configuration.</param>
        /// <returns>A dictionary with all values for one configuration object.</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public IDictionary<string, object> GetValues(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
            if (Context == null) throw new InvalidOperationException($"Property {nameof(Context)} cannot be null.");

            return Materialize(name).ToDictionary(e => e.Name, e => (object) e.Value);
        }

        private IQueryable<ConfigurationEntry> Materialize(string name)
        {
            return Context.ConfigurationEntries
                .AsQueryable()
                .Where(e => e.Section == name);
        }

        /// <summary>Inserts or updates the passed dictionary under the passed name into DbContext.</summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetValues(string name, IDictionary<string, object> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
            if (Context == null) throw new InvalidOperationException($"Property {nameof(Context)} cannot be null.");

            var existingItems = Materialize(name).ToArray();
            
            foreach (var kvp in values)
            {
                var entry = existingItems.FirstOrDefault(i =>
                    string.Equals(i.Name, kvp.Key, StringComparison.InvariantCultureIgnoreCase));

                if (entry == null)
                {
                    entry = new ConfigurationEntry
                    {
                        Section = name, 
                        Name = kvp.Key, 
                        Value = kvp.Value?.ToString()
                    };

                    
                    Context.ConfigurationEntries.Add(entry);
                }
                else
                {
                    entry.Value = kvp.Value?.ToString();
                }                
            }
        }
    }
}