using System;

namespace Swihoni.Components
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CopyField : Attribute
    {
    }

    public static class Copier
    {
        /// <summary>
        /// Set properties on destination that are present on source.
        /// If none exist on source, keep destination values instead of clearing.
        /// </summary>
        public static void FastMergeSet<T>(this T destination, T source) where T : ElementBase
        {
            ElementExtensions.NavigateZipped((_destination, _source) =>
            {
                if (_destination is PropertyBase destinationProperty && _source is PropertyBase sourceProperty)
                    destinationProperty.SetFromIfPresent(sourceProperty);
                return Navigation.Continue;
            }, destination, source);
        }

        /// <summary>
        /// Clears all properties on destination. Then sets properties present on source.
        /// </summary>
        public static void FastCopyFrom<T>(this T destination, T source) where T : ElementBase
        {
            ElementExtensions.NavigateZipped((_destination, _source) =>
            {
                if (_destination is PropertyBase destinationProperty && _source is PropertyBase sourceProperty)
                {
                    destinationProperty.Clear();
                    destinationProperty.SetFromIfPresent(sourceProperty);
                }
                return Navigation.Continue;
            }, destination, source);
        }
    }
}