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

    [AttributeUsage(AttributeTargets.Field)]
    public class Angle : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class Tolerance : Attribute
    {
        public readonly float tolerance;

        public Tolerance(float tolerance)
        {
            this.tolerance = tolerance;
        }
    }

    public static class Interpolator
    {
        public static void InterpolateInto<T>(T e1, T e2, T ed, float interpolation) where T : ElementBase
        {
            Extensions.NavigateZipped((field, _e1, _e2, _ed) =>
            {
                switch (_e1)
                {
                    case PropertyBase p1 when _e2 is PropertyBase p2 && _ed is PropertyBase pd:
                    {
                        if (field != null && field.IsDefined(typeof(TakeSecondForInterpolation)))
                            pd.SetFromIfPresent(p1);
                        else if (field == null || !field.IsDefined(typeof(CustomInterpolation)))
                            pd.InterpolateFromIfPresent(p1, p2, interpolation, field);
                        break;
                    }
                    case ComponentBase c1 when _e2 is ComponentBase c2 && _ed is ComponentBase cd:
                        cd.InterpolateFrom(c1, c2, interpolation);
                        break;
                }
                return Navigation.Continue;
            }, e1, e2, ed);
        }
    }
}