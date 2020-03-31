using System;

namespace Components
{
    [AttributeUsage(AttributeTargets.Field)]
    public class Copy : Attribute
    {
    }

    public static class Copier
    {
        public static void CopyTo(object source, object destination)
        {
            Extensions.Navigate(source, destination, (_source, _destination) => _destination.SetFromIfPresent(_source));
        }
    }
}