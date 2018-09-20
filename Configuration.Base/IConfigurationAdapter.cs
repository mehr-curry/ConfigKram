namespace Configuration
{
    /// <summary>Specifies an interface to transform values from a store to a format which can be used at runtime.</summary>
    public interface IConfigurationAdapter
    {
        bool Exists(object configurationObject);
        void Load(object configurationObject);
        void Save(object configurationObject);
    }
}