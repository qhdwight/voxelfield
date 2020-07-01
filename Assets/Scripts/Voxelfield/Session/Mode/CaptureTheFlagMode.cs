using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Capture The Flag", menuName = "Session/Mode/Capture The Flag", order = 0)]
    public class CaptureTheFlagMode : DeathmatchMode
    {
        public const byte BlueTeam = 0, RedTeam = 1;

        [SerializeField] private LayerMask m_PlayerMask = default;

        [SerializeField] private float m_CaptureRadius = 3.0f;

        private readonly RaycastHit[] m_CachedHits = new RaycastHit[1];
        private readonly Collider[] m_CachedColliders = new Collider[SessionBase.MaxPlayers];
        private FlagBehavior[][] m_TeamFlags = new FlagBehavior[2][];

        private static FlagBehavior[][] GetFlags()
            => MapManager.Singleton.Models.Values
                         .Where(model => model.Container.Require<ModelIdProperty>() == ModelsProperty.Flag)
                         .GroupBy(model => model.Container.Require<TeamProperty>())
                         .Select(group => group.Cast<FlagBehavior>().ToArray()).ToArray();

        public override void Modify(SessionBase session, Container container, uint durationUs)
        {
            base.Modify(session, container, durationUs);

            if (m_TeamFlags == null) m_TeamFlags = GetFlags();
            for (byte team = 0; team < 2; team++)
            {
                for (var flagId = 0; flagId < m_TeamFlags[team].Length; flagId++)
                {
                    HandlePlayersNearFlag(team, flagId);
                }
            }
        }

        private void HandlePlayersNearFlag(byte team, int flagId)
        {
            int count = Physics.OverlapSphereNonAlloc(m_TeamFlags[team][flagId].transform.position, m_CaptureRadius, m_CachedColliders, m_PlayerMask);
        }

        public override void ModifyPlayer(SessionBase session, Container container, int playerId, Container player, Container commands, uint durationUs)
        {
            base.ModifyPlayer(session, container, playerId, player, commands, durationUs);
        }
    }
}