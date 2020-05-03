using Swihoni.Sessions.Components;

namespace Swihoni.Sessions.Interfaces
{
    public class StandardKillFeedInterface : ArrayViewerInterfaceBase<KillFeedEntryInterface, KillFeedProperty, KillFeedComponent>
    {
        protected override bool Less(KillFeedComponent e1, KillFeedComponent e2) { return e1.elapsed < e2.elapsed; }
    }
}