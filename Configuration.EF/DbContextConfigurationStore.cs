using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Configuration.EF
{
    public class DbContextConfigurationStore : IConfigurationStore
    {
        public IConfigurationDbContext Context { get; }

        public DbContextConfigurationStore(IConfigurationDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public IDictionary<string, object> GetValues(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));

            return Context.ConfigurationEntries
                    .Where(e => e.Section == name)
                    .ToDictionary(e => e.Name, e => (object)e.Value);
        }

        public void SetValues(string name, IDictionary<string, object> values)
        {
            
        }
    }
}