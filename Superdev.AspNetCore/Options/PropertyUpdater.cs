using System.Linq.Expressions;
using System.Reflection;

namespace Superdev.AspNetCore.Options
{
    internal sealed class PropertyUpdater<TEntity, TValue>
    {
        private readonly PropertyInfo propertyInfo;

        private PropertyUpdater(PropertyInfo propertyInfo)
        {
            this.propertyInfo = propertyInfo;
        }

        public string Name => this.propertyInfo.Name;

        public static PropertyUpdater<TEntity, TValue> GetPropertyUpdater(Expression<Func<TEntity, TValue>> propertySelector)
        {
            ArgumentNullException.ThrowIfNull(propertySelector);

            if (propertySelector.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("The property selector must target a direct property access.", nameof(propertySelector));
            }

            if (memberExpression.Expression != propertySelector.Parameters[0])
            {
                throw new ArgumentException("Only direct top-level properties are supported.", nameof(propertySelector));
            }

            if (memberExpression.Member is not PropertyInfo propertyInfo)
            {
                throw new ArgumentException("The property selector must target a property.", nameof(propertySelector));
            }

            if (propertyInfo.DeclaringType != typeof(TEntity))
            {
                throw new ArgumentException("Only properties declared on the target options type are supported.", nameof(propertySelector));
            }

            if (!propertyInfo.CanWrite || propertyInfo.SetMethod == null)
            {
                throw new ArgumentException($"Property '{propertyInfo.Name}' must be writable.", nameof(propertySelector));
            }

            if (propertyInfo.GetIndexParameters().Length > 0)
            {
                throw new ArgumentException("Indexed properties are not supported.", nameof(propertySelector));
            }

            return new PropertyUpdater<TEntity, TValue>(propertyInfo);
        }

        public TValue? UpdateValue(TEntity entity, TValue? newValue)
        {
            var oldValue = (TValue?)this.propertyInfo.GetValue(entity);
            this.propertyInfo.SetValue(entity, newValue);
            return oldValue;
        }
    }
}
