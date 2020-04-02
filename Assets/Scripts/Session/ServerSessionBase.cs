namespace Session
{
    public class ServerSessionBase<TSessionComponent> : SessionBase<TSessionComponent> where TSessionComponent : SessionComponentBase
    {
        protected override void Tick(uint tick, float time)
        {
        }
    }
}