using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Configuration
{
    public class ExpressionAdapter : IConfigurationAdapter
    {
        public static IDictionary<Type, Delegate> ExpressionCache { get; } = new Dictionary<Type, Delegate>();
        private readonly IConfigurationStore _store;

        /// <summary>Initializes a new instance with the provided storage object to access an underlying storage.</summary>
        /// <param name="store">A configration storage to get data from or set data to.</param>
        public ExpressionAdapter(IConfigurationStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public T Load<T>() where T : new()
        {
            var result = new T();
            LoadInto(result);
            return result;
        }

        public void LoadInto(object configurationObject)
        {
            if (configurationObject == null) throw new ArgumentNullException(nameof(configurationObject));

            var configObjectType = configurationObject.GetType();
            var values = _store.GetValues(configObjectType.Name);
            var piIndexer = typeof(IDictionary<string, object>).GetProperty("Item", new[] {typeof(string)});
            var expValuesParameter = Expression.Parameter(typeof(IDictionary<string, object>), nameof(values));
            var expObjectParameter = Expression.Parameter(configObjectType, nameof(configurationObject));

            var assignmentList = new List<Expression>();

            foreach (var property in configObjectType.GetProperties())
            {
                var expValuesIndexer =
                    Expression.MakeIndex(expValuesParameter, piIndexer, new[] {Expression.Constant(property.Name)});
                var expTarget = Expression.Property(expObjectParameter, property);
                var expTargetPropType = Expression.Constant(property.PropertyType);
                var miConvertTo =
                    typeof(ExpressionAdapter).GetMethod(nameof(ConvertTo),
                        BindingFlags.Static | BindingFlags.NonPublic);
                var expCallConvert = Expression.Call(miConvertTo, expTargetPropType, expValuesIndexer);
                var expAssign = Expression.Assign(expTarget, Expression.Convert(expCallConvert, property.PropertyType));

                assignmentList.Add(expAssign);
            }

            var expAssignBlock = Expression.Block(assignmentList);

            Expression.Lambda(expAssignBlock, expObjectParameter, expValuesParameter)
                .Compile()
                .DynamicInvoke(configurationObject, values);
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


        public void Save(object configurationObject)
        {
        }
    }
}