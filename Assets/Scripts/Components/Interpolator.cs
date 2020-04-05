using System;
using System.Reflection;

namespace Components
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TakeSecondForInterpolation : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class CustomInterpolation : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class Cyclic : Attribute
    {
        public readonly float minimum, maximum;

        public Cyclic(float minimum, float maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
            if (minimum >= maximum)
                throw new ArgumentException("Minimum equal to or greater than maximum");
        }
    }

    public static class Interpolator
    {
        public static void InterpolateInto<T>(T o1, T o2, T dest, float interpolation)
        {
            Extensions.NavigateZipped((field, _o1, _o2, _destination) =>
            {
                if (field.IsDefined(typeof(CustomInterpolation))) return;
                if (field.IsDefined(typeof(TakeSecondForInterpolation)))
                    _destination.SetFromIfPresent(_o2);
                else
                    _destination.InterpolateFromIfPresent(_o1, _o2, interpolation, field);
            }, o1, o2, dest, (c1, c2, cd) => cd.InterpolateFrom(c1, c2, interpolation));
        }
    }
}