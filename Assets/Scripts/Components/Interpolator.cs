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
        public static void InterpolateInto<T>(T o1, T o2, T destination, float interpolation)
        {
            Extensions.NavigateZipped((field, _o1, _o2, _destination) =>
            {
                if (field.IsDefined(typeof(NoInterpolate)))
                    _destination.SetFromIfPresent(_o2);
                else
                    _destination.InterpolateFromIfPresent(_o1, _o2, interpolation);
            }, o1, o2, destination);
        }
    }
}