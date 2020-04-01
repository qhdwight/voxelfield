using System;

namespace Components
{
    [AttributeUsage(AttributeTargets.Field)]
    public class Copy : Attribute
    {
    }

    public static class Copier
    {
        public static void CopyTo<T>(T source, T destination)
        {
            Extensions.NavigateZipped((_, _source, _destination) => _destination.SetFromIfPresent(_source), source, destination);
        }
    }
}