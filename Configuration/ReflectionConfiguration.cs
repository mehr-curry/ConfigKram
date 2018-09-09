using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Configuration
{
    public class ReflectionConfiguration : IConfiguration
    {
        public string FileName { get; set; }

        public ReflectionConfiguration(string configFile)
        {
            FileName = configFile;
        }

        public void Load(object configurationObject)
        {
            if (configurationObject == null) throw new ArgumentNullException(nameof(configurationObject));

            var configOjectType = configurationObject.GetType();
            var section = GetSection(configOjectType);

            foreach (var property in configOjectType.GetProperties())
            {
                // Wir übergehen die Eigenschaft, wenn es keinen Wert gibt
                if (!section.Children.AllKeys.Contains(property.Name))
                {
                    continue;
                }

                var propertyType = property.PropertyType;
                var isNullable = propertyType.IsByRef;

                // Als nächstes überprüfen wir, ob die Eigenschaft vom Typ Nullable<T> ist.
                // Ggf. nehmen wir das erste Typargument als Typ.
                if (propertyType.IsConstructedGenericType &&
                    propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var genericArgs = propertyType.GetGenericArguments();

                    if (genericArgs.Length != 1)
                    {
                        throw new InvalidOperationException(
                            $"Only {typeof(Nullable<>).Name} with 1 type argument is supported.");
                    }

                    propertyType = genericArgs[0];
                    isNullable = true;
                }

                // wir ermitteln den konfigurierten Wert
                object value = section.Children[property.Name].Value;

                // und prüfen, ob wir ihn konvertieren können/müssen.
                if (propertyType != typeof(string) && 
                    typeof(IConvertible).IsAssignableFrom(propertyType))
                {
                    // sollte die Eigentschaft nullable sein, interpretieren wir
                    // string.Empty als null. Ein leerer String kann leider nicht
                    // per Convert.ChangeType konvertiert werden.
                    if (isNullable && 
                        value is string stringValue &&
                        string.IsNullOrEmpty(stringValue))
                    {
                        value = null;
                    }
                    else
                    {
                        // Anderfalls lassen wir das Framework den Wert konvertieren.
                        value = Convert.ChangeType(value, propertyType);
                    }
                }
                
                // Wenn der Typ der Eigenschaft nullable ist, wird der Wert implizit konvertiert.
                property.SetValue(configurationObject, value);
            }
        }

        public void Save(object configurationObject)
        {
            if (configurationObject == null) throw new ArgumentNullException(nameof(configurationObject));

            var configOjectType = configurationObject.GetType();
            var section = GetSection(configOjectType);

            foreach (var property in configOjectType.GetProperties())
            {
                // element aus der Section holen oder ggf. ein neues anlegen.
                var element = section.Children[property.Name];
                if (element == null)
                {
                    element = new KeyValueConfigurationElement(property.Name, null);
                    section.Children.Add(element);
                }

                // den Wert aus dem Objekt holen
                var value = property.GetValue(configurationObject);
                
                switch (value)
                {
                    case IConvertible convertible:
                        element.Value = Convert.ToString(convertible, CultureInfo.InvariantCulture);
                        break;
                    case null:
                        element.Value = null;
                        break;
                }
            }

            section.CurrentConfiguration.Save();
        }


        private KeyValueConfigurationSection GetSection(Type configOjectType)
        {
            if (configOjectType == null) throw new ArgumentNullException(nameof(configOjectType));
            if(!File.Exists(FileName)) throw new FileNotFoundException("", FileName);
            
            var filemap = new ExeConfigurationFileMap {ExeConfigFilename = FileName};
            var configuration = ConfigurationManager.OpenMappedExeConfiguration(filemap, ConfigurationUserLevel.None);
            var section = configuration.GetSection(configOjectType.Name);

            // gibt es die Section noch nicht, erstellen wir eine neue.
            if (section == null)
            {
                section = new KeyValueConfigurationSection();
                configuration.Sections.Add(configOjectType.Name, section);
            }
            
            var keyValueSection = section as KeyValueConfigurationSection;
            
            return keyValueSection;
        }
    }
}