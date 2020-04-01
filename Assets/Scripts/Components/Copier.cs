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
            Extensions.Navigate((_, properties) => properties[1].SetFromIfPresent(properties[0]), source, destination);
        }
    }
}