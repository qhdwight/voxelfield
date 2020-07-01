using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
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
        [SerializeField] private uint m_TakeFlagDurationUs = 2_000_000u;

        // private readonly RaycastHit[] m_CachedHits = new RaycastHit[1];
        private readonly Collider[] m_CachedColliders = new Collider[SessionBase.MaxPlayers];

        private static FlagBehavior[][] GetFlags()
            => MapManager.Singleton.Models.Values
                         .Where(model => model.Container.Require<ModelIdProperty>() == ModelsProperty.Flag)
                         .GroupBy(model => model.Container.Require<TeamProperty>())
                         .Select(group => group.Cast<FlagBehavior>().ToArray()).ToArray();

        public override void Start(SessionBase session, Container sessionContainer)
        {
            var ctf = sessionContainer.Require<CtfComponent>();
            ctf.teamScores.Zero();
            ctf.teamFlags.Reset();
        }

        public override void Modify(SessionBase session, Container container, uint durationUs)
        {
            base.Modify(session, container, durationUs);

            var ctf = container.Require<CtfComponent>();
            FlagBehavior[][] flags = GetFlags();
            for (byte flagTeam = 0; flagTeam < flags.Length; flagTeam++)
            {
                for (var flagId = 0; flagId < flags[flagTeam].Length; flagId++)
                {
                    FlagComponent flag = ctf.teamFlags[flagTeam][flagId];
                    HandlePlayersNearFlag(session, flags, flag, flagTeam, flagId, ctf);
                    if (flag.captureElapsedTimeUs.WithValue) flag.captureElapsedTimeUs.Value += durationUs;
                }
            }
        }

        private void HandlePlayersNearFlag(SessionBase session, IReadOnlyList<FlagBehavior[]> flagBehaviors, FlagComponent flag, byte flagTeam, int flagId, CtfComponent ctf)
        {
            int count = Physics.OverlapSphereNonAlloc(flagBehaviors[flagTeam][flagId].transform.position, m_CaptureRadius, m_CachedColliders, m_PlayerMask);
            Container enemyTakingInside = null;
            for (var i = 0; i < count; i++)
            {
                if (!m_CachedColliders[i].TryGetComponent(out PlayerTrigger playerTrigger)) continue;

                var playerIdInFlag = (byte) playerTrigger.PlayerId;
                Container player = session.GetPlayerFromId(playerIdInFlag);
                if (player.Require<TeamProperty>() == flagTeam)
                {
                    TryReturnCapturedFlag(flagTeam, ctf, playerTrigger); // Possibly returning captured enemy flag to friendly flag
                }
                else
                {
                    /* Enemy trying to capture flag */
                    if (flag.captureElapsedTimeUs.WithoutValue)
                    {
                        // Start taking
                        flag.capturingPlayerId.Value = playerIdInFlag;
                        flag.captureElapsedTimeUs.Value = 0u;
                        enemyTakingInside = player;
                    }
                    else if (flag.capturingPlayerId == playerIdInFlag && flag.captureElapsedTimeUs < m_TakeFlagDurationUs)
                    {
                        // Enemy in progress of taking
                        enemyTakingInside = player;
                    }
                }
            }
            if (ReferenceEquals(enemyTakingInside, null) || enemyTakingInside.Require<HealthProperty>().IsInactiveOrDead)
            {
                // Return flag if capturing player has left early when taking or has disconnected or died
                flag.Zero();
            }
        }

        private void TryReturnCapturedFlag(byte playerTeam, CtfComponent ctf, PlayerTrigger player)
        {
            for (var flagTeam = 0; flagTeam < ctf.teamFlags.Length; flagTeam++)
            {
                if (flagTeam == playerTeam) continue; // Continue if friendly flag
                FlagArrayElement enemyFlags = ctf.teamFlags[flagTeam];
                foreach (FlagComponent enemyFlag in enemyFlags)
                {
                    if (enemyFlag.capturingPlayerId.WithValue && enemyFlag.capturingPlayerId == player.PlayerId
                                                              && enemyFlag.captureElapsedTimeUs > m_TakeFlagDurationUs) // Test if friendly returning enemy flag
                    {
                        enemyFlag.Reset();
                        ctf.teamScores[playerTeam].Value++;
                    }
                }
            }
        }

        public override void ModifyPlayer(SessionBase session, Container container, int playerId, Container player, Container commands, uint durationUs)
        {
            base.ModifyPlayer(session, container, playerId, player, commands, durationUs);
        }
    }
}