using System.Configuration;

namespace Configuration
{
    public class KeyValueConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty(null, IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(KeyValueConfigurationCollection), AddItemName="add", RemoveItemName="remove", ClearItemsName="clear")]
        public KeyValueConfigurationCollection Children { get => (KeyValueConfigurationCollection)base[string.Empty]; set => base[string.Empty] = value; }
    }
}