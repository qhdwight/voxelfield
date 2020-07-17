using UnityEngine;

namespace Swihoni.Sessions.Player
{
    public class PlayerVisualizerBehavior : PlayerVisualizerBase
    {
        public SkinnedMeshRenderer Renderer { get; private set; }

        public void Setup() => Renderer = GetComponentInChildren<SkinnedMeshRenderer>();
    }
}