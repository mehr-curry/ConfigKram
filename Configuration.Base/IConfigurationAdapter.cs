namespace Configuration
{
    public interface IConfigurationAdapter
    {
        T Load<T>() where T : new();
        void LoadInto(object configurationObject);
        void Save(object configurationObject);
    }
}