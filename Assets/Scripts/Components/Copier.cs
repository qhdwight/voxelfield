using System;

namespace Components
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CopyField : Attribute
    {
    }

    public static class Copier
    {
        public static void MergeSet<T>(T destination, T source)
        {
            Extensions.NavigateZipped((_, _destination, _source) => _destination.SetFromIfPresent(_source), destination, source);
        }
    }
}