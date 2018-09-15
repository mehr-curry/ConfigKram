using System.Collections.Generic;
using System.Globalization;

namespace Configuration
{
    public interface IConfigurationStore
    {
        IDictionary<string, object> GetValues(string name);
        void SetValues(string name, IDictionary<string, object> values);
    }
}