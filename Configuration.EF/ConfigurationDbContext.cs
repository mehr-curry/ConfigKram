using System.Collections.Generic;

namespace Configuration.EF
{
    public class ConfigurationDbContext : IConfigurationDbContext
    {
        public ICollection<ConfigurationEntry> ConfigurationEntries { get; set; }
    }
}