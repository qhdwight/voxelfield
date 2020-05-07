using UnityEngine;

namespace Swihoni.Sessions.Player
{
    public class PlayerVisualizerBehavior : PlayerVisualizerBase
    {
        private SkinnedMeshRenderer m_Renderer;

        public SkinnedMeshRenderer Renderer => m_Renderer;

        public void Setup() { m_Renderer = GetComponentInChildren<SkinnedMeshRenderer>(); }
    }
}