namespace Session
{
    public class ServerSessionBase<TSessionComponent> : SessionBase<TSessionComponent> where TSessionComponent : SessionComponentBase
    {
        public ServerSessionBase(IGameObjectLinker linker) : base(linker)
        {
        }

        protected override void Tick(uint tick, float time)
        {
        }
    }
}