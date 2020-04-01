using System;
using System.Reflection;

namespace Components
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NoInterpolate : Attribute
    {
    }

    public static class Interpolator
    {
        public static void InterpolateInto(object o1, object o2, object destination, float interpolation)
        {
            Extensions.Navigate((field, properties) =>
            {
                PropertyBase destinationProperty = properties[2];
                if (field.IsDefined(typeof(NoInterpolate)))
                    destinationProperty.SetFromIfPresent(properties[1]);
                else
                    destinationProperty.InterpolateFromIfPresent(properties[0], properties[1], interpolation);
            }, o1, o2, destination);
        }
    }
}