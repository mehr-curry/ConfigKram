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

        private readonly IConfigurationStore _store;
        private readonly PropertyInfo _piIndexer =
            typeof(IDictionary<string, object>).GetProperty("Item", new[] {typeof(string)});

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

            // Did we already create a delegate for the configuration objects type?
            if (!LoadExpressionsCache.TryGetValue(configObjectType, out var compiledDelegate))
            {
                var expValuesParameter = Expression.Parameter(typeof(IDictionary<string, object>), nameof(values));
                var expObjectParameter = Expression.Parameter(configObjectType, nameof(configurationObject));
                var assignmentList = new List<Expression>();

                foreach (var property in configObjectType.GetProperties())
                {
                    var expValuesIndexer =
                        Expression.MakeIndex(expValuesParameter, _piIndexer,
                            new[] {Expression.Constant(property.Name)});
                    var expTargetProperty = Expression.Property(expObjectParameter, property);
                    var expTargetPropType = Expression.Constant(property.PropertyType);
                    var expCallConvert = Expression.Call(Adapter.ConvertToMethodInfo, expValuesIndexer, expTargetPropType);
                    var expAssign = Expression.Assign(expTargetProperty,
                        Expression.Convert(expCallConvert, property.PropertyType));

                    assignmentList.Add(expAssign);
                }

                var expAssignBlock = Expression.Block(assignmentList);

                compiledDelegate = Expression.Lambda(expAssignBlock, expObjectParameter, expValuesParameter).Compile();

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
                    var expIndexer = Expression.MakeIndex(expValuesParameter, _piIndexer,
                        new[] {Expression.Constant(property.Name)});

                    assignmentList.Add(
                        Expression.Assign(expIndexer, Expression.Convert(
                            expSourceProperty, _piIndexer.PropertyType)));
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