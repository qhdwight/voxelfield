using Swihoni.Sessions.Components;

namespace Swihoni.Sessions.Interfaces
{
    public class StandardKillFeedInterface : ArrayViewerInterfaceBase<KillFeedEntryInterface, KillFeedElement, KillFeedComponent>
    {
        protected override bool Less(KillFeedComponent e1, KillFeedComponent e2)
        {
            if (e1.elapsedUs.WithoutValue) return true;
            if (e2.elapsedUs.WithoutValue) return false;
            return e1.elapsedUs < e2.elapsedUs;
        }

        public override void Render(in SessionContext context)
        {
            SetInterfaceActive(true);
            base.Render(context);
        }
    }
}