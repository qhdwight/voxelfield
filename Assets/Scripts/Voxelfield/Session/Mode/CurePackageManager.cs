using Swihoni.Components;
using Swihoni.Sessions;

namespace Voxelfield.Session.Mode
{
    public class CurePackageManager : BehaviorManagerBase
    {
        public CurePackageManager(int count, string resourceFolder) : base(count, resourceFolder)
        {
        }

        public override ArrayElementBase ExtractArray(Container session) => session.Require<ShowdownSessionComponent>().curePackages;
    }
}