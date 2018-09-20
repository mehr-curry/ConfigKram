using System.Collections.Generic;
using System.Globalization;

namespace Configuration
{
    /// <summary>Specifies an interface for accessing different type of configuration stores.</summary>
    public interface IConfigurationStore
    {
        IDictionary<string, object> GetValues(string name);
        void SetValues(string name, IDictionary<string, object> values);
    }
}