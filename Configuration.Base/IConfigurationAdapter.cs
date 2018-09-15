namespace Configuration
{
    public interface IConfigurationAdapter
    {
        void Load(object configurationObject);
        void Save(object configurationObject);
    }
}