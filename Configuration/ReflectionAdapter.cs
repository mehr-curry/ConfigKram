using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Configuration
{
    /// <summary>A ConfigurationAdapter implementation which uses reflection to fill an object with data or retrieve data from an object.</summary>
    public class ReflectionAdapter : IConfigurationAdapter
    {
        private readonly IConfigurationStore _store;

        /// <summary>Initializes a new instance with the provided storage object to access an underlying storage.</summary>
        /// <param name="store">A configration storage to get data from or set data to.</param>
        public ReflectionAdapter(IConfigurationStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool Exists(object configurationObject)
        {
            throw new NotImplementedException();
	}

        public T Load<T>()
            where T : new()
        {
            var result = new T();
            LoadInto(result);
            return result;
        }

        /// <summary>Loads configuration data into the passed instance.</summary>
        /// <param name="configurationObject">The object which has to be filled with data.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="configurationObject"/> is null.</exception>
        /// <exception cref="InvalidOperationException">If a property is of type Nullable and defines not exactly 1 type parameter.</exception>
        public void LoadInto(object configurationObject)
        {
            if (configurationObject == null) throw new ArgumentNullException(nameof(configurationObject));

            var configObjectType = configurationObject.GetType();
            var values = _store.GetValues(configObjectType.Name);

            foreach (var property in configObjectType.GetProperties())
            {
                // We retrieve the value from the storages return value.
                // If there is no entry we will skip the property.
                if (!values.TryGetValue(property.Name, out var value))
                {
                    continue;
                }

                var propertyType = property.PropertyType;
                
                property.SetValue(configurationObject, 
                                  ConvertTo(propertyType, value));
            }
        }

        private static object ConvertTo(Type propertyType, object value)
        {
            var isNullable = propertyType.IsByRef;

            // As the next step we will check whether the properties type is
            // is Nullable<T>. If so we will take the type argument as our 
            // target type
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

            // We are going to convert the raw value from the storage into something
            // the property will accept.
            if (propertyType != typeof(string) &&
                typeof(IConvertible).IsAssignableFrom(propertyType))
            {
                // If the property is nullable, we accept String.Empty as null.
                // Convert.ChangeType cannot convert String.Empty into a primitve value.
                if (isNullable &&
                    value is string stringValue &&
                    string.IsNullOrEmpty(stringValue))
                {
                    value = null;
                }
                else
                {
                    // otherwise we use the frameworks capabilities to 
                    // convert the string into our target type
                    value = System.Convert.ChangeType(value, propertyType);
                }
            }

            return value;
        }

        /// <summary>Stores the passed object into a configuration storage.</summary>
        /// <param name="configurationObject">The object with data which has to be stored.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="configurationObject"/> is null.</exception>
        public void Save(object configurationObject)
        {
            if (configurationObject == null) throw new ArgumentNullException(nameof(configurationObject));

            var configOjectType = configurationObject.GetType();
            var values = new Dictionary<string, object>(); //_store.GetValues(configOjectType.Name));

            foreach (var property in configOjectType.GetProperties())
            {
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
