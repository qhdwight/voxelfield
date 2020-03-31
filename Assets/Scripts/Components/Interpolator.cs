namespace Components
{
    public static class Interpolator
    {
        public static void InterpolateInto(object source, object destination, float interpolation)
        {
            Extensions.Navigate(source, destination, (_source, _destination) => _destination.InterpolateFromIfPresent(_source, interpolation));
        }
    }
}