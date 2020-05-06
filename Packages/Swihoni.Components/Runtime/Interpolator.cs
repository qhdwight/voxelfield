using System;
using System.Reflection;

namespace Swihoni.Components
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TakeSecondForInterpolationAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class CustomInterpolationAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class CyclicAttribute : Attribute
    {
        public readonly float minimum, maximum;

        public CyclicAttribute(float minimum, float maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
            if (minimum >= maximum)
                throw new ArgumentException("Minimum equal to or greater than maximum");
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class AngleAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ToleranceAttribute : Attribute
    {
        public readonly float tolerance;

        public ToleranceAttribute(float tolerance) => this.tolerance = tolerance;
    }

    /// <summary>
    /// Interpolate field only if two given values fall within a certain range.
    /// An example usage would be with player respawn, where the position of the player is abruptly changed.
    /// We do not want to interpolate the player going back to spawn.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InterpolateRangeAttribute : Attribute
    {
        public readonly float range;

        public InterpolateRangeAttribute(float range) => this.range = range;
    }

    public static class Interpolator
    {
        public static void InterpolateInto<T>(T e1, T e2, T ed, float interpolation) where T : ElementBase
        {
            ElementExtensions.NavigateZipped((_e1, _e2, _ed) =>
            {
                switch (_e1)
                {
                    case PropertyBase p1 when _e2 is PropertyBase p2 && _ed is PropertyBase pd:
                    {
                        pd.InterpolateFromIfPresent(p1, p2, interpolation);
                        break;
                    }
                    case ComponentBase c1 when _e2 is ComponentBase c2 && _ed is ComponentBase cd:
                        cd.InterpolateFrom(c1, c2, interpolation);
                        if (c1.GetType().IsDefined(typeof(CustomInterpolationAttribute)))
                            return Navigation.SkipDescendends;
                        break;
                }
                return Navigation.Continue;
            }, e1, e2, ed);
        }
    }
}