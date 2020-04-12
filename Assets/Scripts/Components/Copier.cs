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
            Extensions.NavigateZipped((_, _destination, _source) => _destination.SetFromIfPresent(_source), destination, source);
        }
    }
}