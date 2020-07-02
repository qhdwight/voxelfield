using Swihoni.Sessions.Player.Components;
using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    public class FlagBehavior : ModelBehavior
    {
        private Cloth m_FlagCloth;
        private SkinnedMeshRenderer m_FlagRenderer;
        private Material m_Material;

        private void Awake()
        {
            m_FlagCloth = GetComponentInChildren<Cloth>();
            m_FlagRenderer = m_FlagCloth.GetComponent<SkinnedMeshRenderer>();
        }

        public void Render(FlagComponent flag)
        {
            if (!m_Material) m_Material = m_FlagRenderer.material;
            m_FlagRenderer.enabled = flag.capturingPlayerId.WithoutValue;
            m_Material.color = Container.Require<TeamProperty>() == CtfMode.BlueTeam ? Color.blue : Color.red;
        }
    }
}