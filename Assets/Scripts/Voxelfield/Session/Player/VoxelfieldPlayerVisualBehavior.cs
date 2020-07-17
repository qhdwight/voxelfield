using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Player.Visualization;
using UnityEngine;

namespace Voxelfield.Session.Player
{
    public class VoxelfieldPlayerVisualBehavior : PlayerVisualsBehaviorBase
    {
        [SerializeField] private AudioSource m_BreakVoxelAudioSource = default;

        private byte m_LastBreakTick;

        public override void SetActive(bool isActive) => m_LastBreakTick = byte.MaxValue;

        public override void Render(SessionBase session, Container player, bool isLocalPlayer)
        {
            if (!m_BreakVoxelAudioSource) return;
            if (player.WithPropertyWithValue(out BrokeVoxelTickProperty breakTick))
            {
                m_BreakVoxelAudioSource.pitch = Random.Range(0.8f, 1.2f);
                m_BreakVoxelAudioSource.volume = Random.Range(0.8f, 1.0f);
                if (breakTick != m_LastBreakTick)
                {
                    m_BreakVoxelAudioSource.Play();
                    m_LastBreakTick = breakTick;
                }
            }
        }
    }
}