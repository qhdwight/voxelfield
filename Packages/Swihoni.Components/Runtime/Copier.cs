using System;

namespace Swihoni.Components
{
    /// <summary>
    /// Use on private fields to explicitly copy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class CopyField : Attribute
    {
    }

    public static class Copier
    {
        /// <summary>
        /// Set properties on destination that are with values.
        /// If a property on the source is without a value, keep destination value instead of clearing.
        /// Use <see cref="CopyFrom{T}"/> if you prefer to clear instead.
        /// </summary>
        public static void MergeFrom<T>(this T destination, T source) where T : ElementBase
        {
            ElementExtensions.NavigateZipped((_destination, _source) =>
            {
                if (_destination is PropertyBase destinationProperty && _source is PropertyBase sourceProperty)
                    destinationProperty.SetFromIfWith(sourceProperty);
                return Navigation.Continue;
            }, destination, source);
        }

        /// <summary>
        /// Clears all properties on destination. Then sets from properties on the source with values.
        /// </summary>
        public static void CopyFrom<T>(this T destination, T source) where T : ElementBase
        {
            ElementExtensions.NavigateZipped((_destination, _source) =>
            {
                if (_destination is PropertyBase destinationProperty && _source is PropertyBase sourceProperty)
                    destinationProperty.SetTo(sourceProperty);
                return Navigation.Continue;
            }, destination, source);
        }
    }
}