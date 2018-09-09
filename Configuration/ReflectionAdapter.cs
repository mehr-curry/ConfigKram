using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Configuration
{
    public class ReflectionAdapter : IConfigurationAdapter
    {
        private readonly IConfigurationStore _store;

        public ReflectionAdapter(IConfigurationStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public void Load(object configurationObject)
        {
            if (configurationObject == null) throw new ArgumentNullException(nameof(configurationObject));

            var configOjectType = configurationObject.GetType();
            var values = _store.GetValues(configOjectType.Name);

            foreach (var property in configOjectType.GetProperties())
            {
                // Wir übergehen die Eigenschaft, wenn es keinen Wert gibt
                if (!values.TryGetValue(property.Name, out var value))
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
            var values = _store.GetValues(configOjectType.Name);

            foreach (var property in configOjectType.GetProperties())
            {
                // element aus der Section holen oder ggf. ein neues anlegen.
//                var element = values[property.Name];
//                if (element == null)
//                {
//                    element = new KeyValueConfigurationElement(property.Name, null);
//                    values.Children.Add(element);
//                }

                // den Wert aus dem Objekt holen
                var value = property.GetValue(configurationObject);
                
                switch (value)
                {
                    case IConvertible convertible:
                        value = Convert.ToString(convertible, CultureInfo.InvariantCulture);
                        break;
                }

                values[property.Name] = value;
            }

            _store.SetValues(configOjectType.Name, values);
        }


        
    }
}