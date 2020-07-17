using Swihoni.Components;
using Swihoni.Sessions.Components;

namespace Swihoni.Sessions.Interfaces
{
    public class StandardKillFeedInterface : ArrayViewerInterfaceBase<KillFeedEntryInterface, KillFeedElement, KillFeedComponent>
    {
        protected override bool Less(KillFeedComponent e1, KillFeedComponent e2) => e1.elapsedUs < e2.elapsedUs;

        public override void Render(SessionBase session, Container sessionContainer)
        {
            SetInterfaceActive(true);
            base.Render(session, sessionContainer);
        }
    }
}