using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Voxelfield.Session.Mode
{
    public class SpawnBehavior : ModelBehavior
    {
        private Material m_Material;

        private void Awake() { m_Material = GetComponentInChildren<SkinnedMeshRenderer>().material; }

        public override void SetInMode(Container session)
        {
            gameObject.SetActive(session.Require<ModeIdProperty>() == ModeIdProperty.Designer);
            m_Material.color = FlagBehavior.GetTeamColor(Container);
        }
    }
}