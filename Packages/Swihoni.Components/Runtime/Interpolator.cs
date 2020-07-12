using System;

namespace Swihoni.Components
{
    /// <summary>
    /// Use on any <see cref="PropertyBase"/>.
    /// When interpolating, set property value to the second one.
    /// This is useful for properties that don't have a well defined interpolation function.
    /// TODO:feature allow for all elements not just properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TakeSecondForInterpolationAttribute : Attribute
    {
    }

    /// <summary>
    /// Use on any <see cref="ElementBase"/>.
    /// When interpolating, skip descendents of component.
    /// The assumption is that the component has a proper <see cref="ComponentBase.InterpolateFrom"/> which provides custom interpolation for descendents.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class CustomInterpolationAttribute : Attribute
    {
    }

    /// <summary>
    /// Use on any <see cref="FloatProperty"/>.
    /// Treat number line as cyclic, when <see cref="maximum"/> is reached, loop back to <see cref="minimum"/>.
    /// Useful for times that describe a looping action. For example, player idle state.
    /// </summary>
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

    /// <summary>
    /// Use on any <see cref="FloatProperty"/> describing an angle.
    /// Interpolates an angle in degrees properly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AngleAttribute : Attribute
    {
    }

    /// <summary>
    /// Use on any <see cref="FloatProperty"/> or <see cref="VectorProperty"/>.
    /// Used in comparisons between two properties to see if they are equal.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ToleranceAttribute : Attribute
    {
        public readonly float tolerance;

        public ToleranceAttribute(float tolerance) => this.tolerance = tolerance;
    }

    public class PredictionToleranceAttribute : ToleranceAttribute
    {
        public PredictionToleranceAttribute(float tolerance) : base(tolerance) { }
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
        private static float _interpolation;
        
        public static void InterpolateInto<T>(T e1, T e2, T ed, float interpolation) where T : ElementBase
        {
            _interpolation = interpolation; // Prevent closure allocation
            ElementExtensions.NavigateZipped((_e1, _e2, _ed) =>
            {
                switch (_e1)
                {
                    case PropertyBase p1 when _e2 is PropertyBase p2 && _ed is PropertyBase pd:
                    {
                        pd.InterpolateFromIfWith(p1, p2, _interpolation);
                        break;
                    }
                    case ComponentBase c1 when _e2 is ComponentBase c2 && _ed is ComponentBase cd:
                        if (c1.WithAttribute<CustomInterpolationAttribute>())
                        {
                            cd.InterpolateFrom(c1, c2, _interpolation);
                            return Navigation.SkipDescendents;
                        }
                        break;
                }
                return Navigation.Continue;
            }, e1, e2, ed);
        }
    }
}