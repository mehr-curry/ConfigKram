using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Configuration
{
    /// <summary>A ConfigurationAdapter implementation which uses Expression trees to fill an object with data or retrieve data from an object. </summary>
    public class ExpressionAdapter : IConfigurationAdapter
    {
        private static IDictionary<Type, Delegate> LoadExpressionsCache { get; } = new Dictionary<Type, Delegate>();
        private static IDictionary<Type, Delegate> SaveExpressionsCache { get; } = new Dictionary<Type, Delegate>();

        /// <summary>Stores the property info for idictionary<string, object>'s indexer.</summary>
        /// <remarks>It will not change over the life time of the object hence we are declaring it as a static field.</remarks>
        private static readonly PropertyInfo PropertyInfoIndexer =
            typeof(IDictionary<string, object>).GetProperty("Item", new[] {typeof(string)});

        /// <summary>Stores a reference to the underlying configuration store which will provide access to the configuration values.</summary>
        private readonly IConfigurationStore _store;

        /// <summary>Initializes a new instance with the provided storage object to access an underlying storage.</summary>
        /// <param name="store">A configration storage to get data from or set data to.</param>
        public ExpressionAdapter(IConfigurationStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool Exists(object configurationObject)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IConfigurationAdapter.Load<T>"/>
        public T Load<T>() where T : new()
        {
            var result = new T();
            LoadInto(result);
            return result;
        }

        /// <inheritdoc cref="IConfigurationAdapter.LoadInto"/>
        public void LoadInto(object configurationObject)
        {
            if (configurationObject == null) throw new ArgumentNullException(nameof(configurationObject));

            var configObjectType = configurationObject.GetType();
            var values = _store.GetValues(configObjectType.Name);

            // Did we already create a delegate for the configuration objects type?
            if (!LoadExpressionsCache.TryGetValue(configObjectType, out var compiledDelegate))
            {
                // these will be the input parameters for our dynamic function
                // parameter 1: IDictionary<string, object> values
                var expValuesParameter = Expression.Parameter(typeof(IDictionary<string, object>), nameof(values));
                // parameter 2: TConfigurationObject configurationObject
                var expObjectParameter = Expression.Parameter(configObjectType, nameof(configurationObject));
                // this one will contain all assignments from storage to configuration object
                // before we are going create a block for them
                var assignmentList = new List<Expression>();

                // we resolve all necessary properties via reflection - but only once per type
                foreach (var property in configObjectType.GetProperties())
                {
                    var expValuesIndexer =
                        Expression.MakeIndex(expValuesParameter, PropertyInfoIndexer,
                            new[] {Expression.Constant(property.Name)});
                    
                    var expTargetProperty = Expression.Property(expObjectParameter, property);
                    var expTargetPropType = Expression.Constant(property.PropertyType);
                    var expCallConvert = Expression.Call(Adapter.ConvertToMethodInfo, expValuesIndexer, expTargetPropType);
                    // configurationObject.Property = Adapter.ConvertToMethodInfo(values[mame], propertyType)
                    var expAssign = Expression.Assign(expTargetProperty,
                        Expression.Convert(expCallConvert, property.PropertyType));

                    assignmentList.Add(expAssign);
                }

                // creates a block for all assignments
                // {
                //    configurationObject.Property1 = Adapter.ConvertToMethodInfo(values[mame1], propertyType1)
                //    ...
                //    configurationObject.PropertyN = Adapter.ConvertToMethodInfo(values[mameN], propertyTypeN)
                // }
                var expAssignBlock = Expression.Block(assignmentList);

                // create the final expression and a delegate.
                compiledDelegate = Expression.Lambda(expAssignBlock, expObjectParameter, expValuesParameter).Compile();

                // at last we have to store the compiled delegate for late so we are able to reuse it
                LoadExpressionsCache.Add(configObjectType, compiledDelegate);
            }

            compiledDelegate.DynamicInvoke(configurationObject, values);
        }


        public void Save(object configurationObject)
        {
            if (configurationObject == null) throw new ArgumentNullException(nameof(configurationObject));
            
            var configObjectType = configurationObject.GetType();
            var values = new Dictionary<string, object>();
            
            // Did we already create a delegate for the configuration objects type?
            if (!SaveExpressionsCache.TryGetValue(configObjectType, out var compiledDelegate))
            {
                var expValuesParameter = Expression.Parameter(typeof(IDictionary<string, object>), nameof(values));
                var expObjectParameter = Expression.Parameter(configObjectType, nameof(configurationObject));
                var assignmentList = new List<Expression>();
                
                foreach (var property in configObjectType.GetProperties())
                {
                    var expSourceProperty = Expression.Property(expObjectParameter, property);
                    var expIndexer = Expression.MakeIndex(expValuesParameter, PropertyInfoIndexer,
                        new[] {Expression.Constant(property.Name)});

                    assignmentList.Add(
                        Expression.Assign(expIndexer, Expression.Convert(
                            expSourceProperty, PropertyInfoIndexer.PropertyType)));
                }
                
                var expAssignBlock = Expression.Block(assignmentList);

                compiledDelegate = Expression.Lambda(expAssignBlock, expObjectParameter, expValuesParameter).Compile();

                SaveExpressionsCache.Add(configObjectType, compiledDelegate);
            }

            compiledDelegate.DynamicInvoke(configurationObject, values);
            
            _store.SetValues(configObjectType.Name, values);
        }
    }
}