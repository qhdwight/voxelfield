using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Voxelfield.Session.Mode
{
    public class CurePackageBehavior : ModelBehavior
    {
        [SerializeField] private GameObject m_Cure = default;

        public void Render(CurePackageComponent cure)
        {
            if (cure.isActive.WithoutValue)
            {
                gameObject.SetActive(false);
                return;
            }
            bool isCureActive = cure.isActive;
            m_Cure.SetActive(isCureActive);
        }

        public override void SetInMode(Container session) => gameObject.SetActive(IsModeOrDesigner(session, ModeIdProperty.Showdown));
    }
}