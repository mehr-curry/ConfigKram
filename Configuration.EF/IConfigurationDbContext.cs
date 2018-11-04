using System.Collections.Generic;

namespace Configuration.EF
{
    public interface IConfigurationDbContext
    {
        ICollection<ConfigurationEntry> ConfigurationEntries { get; set; }
    }
}