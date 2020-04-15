using System;

namespace Components
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CopyField : Attribute
    {
    }

    public static class Copier
    {
        public static void MergeSet<T>(this T destination, T source) where T : ElementBase
        {
            Extensions.NavigateZipped((_, _destination, _source) =>
            {
                if (_destination is PropertyBase destinationProperty && _source is PropertyBase sourceProperty)
                    destinationProperty.SetFromIfPresent(sourceProperty);
                return Navigation.Continue;
            }, destination, source);
        }
    }
}