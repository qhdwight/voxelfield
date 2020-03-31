namespace Session
{
    public class ServerSessionBase<TSessionState> : SessionBase<TSessionState> where TSessionState : SessionStateComponentBase
    {
        protected override void Tick(uint tick, float time)
        {
        }
    }
}