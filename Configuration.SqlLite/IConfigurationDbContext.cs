using System.Collections.Generic;

namespace Configuration.SqlLite
{
    public interface IConfigurationContext
    {
        ICollection<ConfigurationEntry> ConfigurationEntries { get; set; }
    }
}