using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Capture The Flag", menuName = "Session/Mode/Capture The Flag", order = 0)]
    public class CtfMode : DeathmatchModeBase
    {
        public const byte BlueTeam = 0, RedTeam = 1;

        public const uint TakeFlagDurationUs = 2_000_000u;

        [SerializeField] private LayerMask m_PlayerMask = default;

        [SerializeField] private float m_CaptureRadius = 3.0f;

        // private readonly RaycastHit[] m_CachedHits = new RaycastHit[1];
        private readonly Collider[] m_CachedColliders = new Collider[SessionBase.MaxPlayers];
        private FlagBehavior[][] m_FlagBehaviors;

        private void LinkFlagBehaviors()
            => m_FlagBehaviors = MapManager.Singleton.Models.Values
                                           .Where(model => model.Container.Require<ModelIdProperty>() == ModelsProperty.Flag)
                                           .GroupBy(model => model.Container.Require<TeamProperty>().Value)
                                           .Select(group => group.Cast<FlagBehavior>().ToArray()).ToArray();

        public override void Begin(SessionBase session, Container sessionContainer)
        {
            Debug.Log("Starting capture the flag game mode");
            var ctf = sessionContainer.Require<CtfComponent>();
            ctf.teamScores.Zero();
            ctf.teamFlags.Clear();
            m_FlagBehaviors = null;
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            if (m_FlagBehaviors == null && !session.IsPaused) LinkFlagBehaviors();
            if (m_FlagBehaviors == null) return;

            ArrayElement<FlagArrayElement> flags = sessionContainer.Require<CtfComponent>().teamFlags;
            for (var flagTeam = 0; flagTeam < m_FlagBehaviors.Length; flagTeam++)
            for (var flagId = 0; flagId < m_FlagBehaviors[flagTeam].Length; flagId++)
                m_FlagBehaviors[flagTeam][flagId].Render(session, sessionContainer, flags[flagTeam][flagId]);
        }

        public override void Modify(SessionBase session, Container container, uint durationUs)
        {
            base.Modify(session, container, durationUs);

            if (m_FlagBehaviors == null && !session.IsPaused) LinkFlagBehaviors();
            if (m_FlagBehaviors == null) return;

            var ctf = container.Require<CtfComponent>();
            for (byte flagTeam = 0; flagTeam < m_FlagBehaviors.Length; flagTeam++)
            for (var flagId = 0; flagId < m_FlagBehaviors[flagTeam].Length; flagId++)
            {
                FlagComponent flag = ctf.teamFlags[flagTeam][flagId];
                HandlePlayersNearFlag(session, flag, flagTeam, flagId, ctf);
                if (flag.captureElapsedTimeUs.WithValue) flag.captureElapsedTimeUs.Value += durationUs;
            }
        }

        protected override float CalculateWeaponDamage(SessionBase session, Container hitPlayer, Container inflictingPlayer, PlayerHitbox hitbox, WeaponModifierBase weapon,
                                                       in RaycastHit hit)
        {
            if (hitPlayer.Require<TeamProperty>() == inflictingPlayer.Require<TeamProperty>()) return 0.0f;

            float baseDamage = base.CalculateWeaponDamage(session, hitPlayer, inflictingPlayer, hitbox, weapon, hit);
            return ShowdownMode.CalculateDamageWithMovement(session, inflictingPlayer, weapon, baseDamage);
        }

        private void HandlePlayersNearFlag(SessionBase session, FlagComponent flag, byte flagTeam, int flagId, CtfComponent ctf)
        {
            int count = Physics.OverlapSphereNonAlloc(m_FlagBehaviors[flagTeam][flagId].transform.position, m_CaptureRadius, m_CachedColliders, m_PlayerMask);
            Container enemyTakingIn = null;
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
                    if (flag.capturingPlayerId.WithoutValue)
                    {
                        // Start taking
                        flag.capturingPlayerId.Value = playerIdInFlag;
                        flag.captureElapsedTimeUs.Value = 0u;
                        enemyTakingIn = player;
                    }
                    else if (flag.capturingPlayerId == playerIdInFlag)
                    {
                        // Enemy in progress of taking
                        enemyTakingIn = player;
                    }
                }
            }
            Container enemyTaking;
            if (ReferenceEquals(enemyTakingIn, null) && flag.capturingPlayerId.WithValue)
            {
                // Flag is taken, but the capturing player is outside the radius of taking
                enemyTaking = session.GetPlayerFromId(flag.capturingPlayerId);
                // Return flag if capturing player disconnects / dies or if they have not fully taken
                if (enemyTaking.Require<HealthProperty>().IsInactiveOrDead || flag.captureElapsedTimeUs < TakeFlagDurationUs) enemyTaking = null;
            }
            else enemyTaking = enemyTakingIn;
            if (ReferenceEquals(enemyTaking, null))
            {
                // Return flag if capturing player has left early when taking or has disconnected or died
                flag.Clear();
            }
        }

        private static void TryReturnCapturedFlag(byte playerTeam, CtfComponent ctf, PlayerTrigger player)
        {
            for (var flagTeam = 0; flagTeam < ctf.teamFlags.Length; flagTeam++)
            {
                if (flagTeam == playerTeam) continue; // Continue if friendly flag
                FlagArrayElement enemyFlags = ctf.teamFlags[flagTeam];
                foreach (FlagComponent enemyFlag in enemyFlags)
                {
                    if (enemyFlag.capturingPlayerId.WithValue && enemyFlag.capturingPlayerId == player.PlayerId
                                                              && enemyFlag.captureElapsedTimeUs > TakeFlagDurationUs) // Test if friendly returning enemy flag
                    {
                        enemyFlag.Clear();
                        ctf.teamScores[playerTeam].Value++;
                    }
                }
            }
        }

        protected override void SpawnPlayer(SessionBase session, int playerId, Container player)
        {
            base.SpawnPlayer(session, playerId, player);
            player.Require<TeamProperty>().Value = (byte) (playerId % 2);
        }

        public override void ModifyPlayer(SessionBase session, Container container, int playerId, Container player, Container commands, uint durationUs, int tickDelta)
        {
            base.ModifyPlayer(session, container, playerId, player, commands, durationUs, tickDelta);
        }
    }
}