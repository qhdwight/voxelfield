using Swihoni.Sessions.Components;

namespace Swihoni.Sessions.Interfaces
{
    public class StandardKillFeedInterface : ArrayViewerInterfaceBase<KillFeedEntryInterface, KillFeedElement, KillFeedComponent>
    {
        protected override int Compare(KillFeedComponent e1, KillFeedComponent e2)
        {
            if (e1.elapsedUs.WithoutValue) return -1;
            if (e2.elapsedUs.WithoutValue) return 1;
            return e1.elapsedUs.Value.CompareTo(e2.elapsedUs.Value);
        }

        public override void Render(in SessionContext context)
        {
            SetInterfaceActive(true);
            base.Render(context);
        }
    }
}