using Swihoni.Sessions.Components;

namespace Swihoni.Sessions.Interfaces
{
    public class StandardKillFeedInterface : ArrayViewerInterfaceBase<KillFeedEntryInterface, KillFeedElement, KillFeedComponent>
    {
        protected override int Compare(KillFeedComponent e1, KillFeedComponent e2)
        {
            if (e1.timeUs.WithoutValue) return -1;
            if (e2.timeUs.WithoutValue) return 1;
            return e1.timeUs.Value.CompareTo(e2.timeUs.Value);
        }

        public override void Render(in SessionContext context)
        {
            SetInterfaceActive(true);
            base.Render(context);
        }
    }
}