namespace Session
{
    public interface ISessionVisualizer
    {
        void Visualize(SessionState session);
    }

    public interface ISessionModifier
    {
        void Modify(SessionState session);
    }
}