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
        public static void InterpolateInto<T>(T e1, T e2, T dest, float interpolation) where T : ElementBase
        {
            Extensions.NavigateZipped((field, _e1, _e2, _destination) =>
            {
                if (field == null) return;
                if (field.IsDefined(typeof(CustomInterpolation))) return;
                if (field.IsDefined(typeof(TakeSecondForInterpolation)))
                    _destination.SetFromIfPresent(_e2);
                else
                    _destination.InterpolateFromIfPresent(_e1, _e2, interpolation, field);
            }, e1, e2, dest);
        }
    }
}