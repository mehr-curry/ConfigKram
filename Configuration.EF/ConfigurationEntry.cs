namespace Configuration.EF
{
    /// <summary>Stores one configuration entry for one specific configuration object</summary>
    public class ConfigurationEntry
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Section { get; set; }
    }
}