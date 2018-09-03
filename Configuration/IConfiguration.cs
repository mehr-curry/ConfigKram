namespace Configuration
{
    public interface IConfiguration
    {
        void Load(object configurationObject);
        void Save(object configurationObject);
    }
}