using Swihoni.Components;
using Swihoni.Sessions.Components;

namespace Voxelfield.Session.Mode
{
    public class SiteBehavior : ModelBehavior
    {
        public override void SetInMode(Container session) => gameObject.SetActive(IsModeOrDesigner(session, ModeIdProperty.SecureArea));

        public void Render(FlagComponent flag) {  }
    }
}