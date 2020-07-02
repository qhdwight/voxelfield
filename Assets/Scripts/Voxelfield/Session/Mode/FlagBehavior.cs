using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    public class FlagBehavior : ModelBehavior
    {
        private Cloth m_FlagCloth;
        private SkinnedMeshRenderer m_FlagRenderer;
        private Material m_Material;

        [SerializeField] private float m_TakingBlinkRate = 10.0f;

        private void Awake()
        {
            m_FlagCloth = GetComponentInChildren<Cloth>();
            m_FlagRenderer = m_FlagCloth.GetComponent<SkinnedMeshRenderer>();
        }

        public void Render(FlagComponent flag)
        {
            if (!m_Material) m_Material = m_FlagRenderer.material;
            Color color = Container.Require<TeamProperty>() == CtfMode.BlueTeam ? Color.blue : Color.red;
            color.a = Mathf.Cos(flag.captureElapsedTimeUs.Else(0u) * TimeConversions.MicrosecondToSecond * m_TakingBlinkRate).Remap(-1.0f, 1.0f, 0.8f, 1.0f);
            m_Material.color = color;
        }
    }
}