using System;
using System.Reflection;

namespace Configuration
{
    public static class Adapter
    {
        public static readonly MethodInfo ConvertToMethodInfo =
            typeof(Adapter).GetMethod(nameof(Adapter.ConvertTo), BindingFlags.Static | BindingFlags.Public);

        /// <summary>Helper to convert a value into the specified target type.</summary>
        /// <param name="value">The original value.</param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>String.Empty will be treated as null if the target property is nullable.</remarks>
        public static object ConvertTo(object value, Type targetType)
        {
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var isNullable = targetType.IsByRef;

            // As the next step we will check whether the properties type is
            // is Nullable<T>. If so we will take the type argument as our 
            // target type
            if (targetType.IsConstructedGenericType &&
                targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var genericArgs = targetType.GetGenericArguments();

                if (genericArgs.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Only {typeof(Nullable<>).Name} with 1 type argument is supported.");
                }

                targetType = genericArgs[0];
                isNullable = true;
            }

            // We are going to convert the raw value from the storage into something
            // the property will accept.
            if (targetType != typeof(string) &&
                typeof(IConvertible).IsAssignableFrom(targetType))
            {
                // If the property is nullable, we accept String.Empty as null.
                // Convert.ChangeType cannot convert String.Empty into a primitive value.
                if (isNullable &&
                    value is string stringValue &&
                    String.IsNullOrEmpty(stringValue))
                {
                    value = null;
                }
                else
                {
                    // otherwise we use the frameworks capabilities to 
                    // convert the string into our target type
                    value = System.Convert.ChangeType(value, targetType);
                }
            }

            return value;
        }
    }
}