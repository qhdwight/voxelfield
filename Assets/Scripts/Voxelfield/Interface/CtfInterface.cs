using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using UnityEngine.UI;
using Voxelfield.Interface.Showdown;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield.Interface
{
    public class CtfInterface : SessionInterfaceBehavior
    {
        [SerializeField] private ProgressInterface m_TakeProgress = default;
        [SerializeField] private UpperScoreInterface m_ScoreInterface = default;
        [SerializeField] private Image m_HasFlagSprite = default;
        [SerializeField] private BufferedTextGui m_RespawnText = default;
        private LoadOutInterface m_LoadOutInterface;

        protected override void Awake()
        {
            base.Awake();
            m_LoadOutInterface = GetComponentInChildren<LoadOutInterface>();
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Ctf;
            if (isVisible)
            {
                var ctf = sessionContainer.Require<CtfComponent>();
                RenderLocalPlayer(session, sessionContainer, ctf);

                var mode = (CtfMode) ModeManager.GetMode(ModeIdProperty.Ctf);
                var teamScores = sessionContainer.Require<DualScoresComponent>();
                m_ScoreInterface.Render(teamScores[CtfMode.BlueTeam], mode.GetTeamColor(CtfMode.BlueTeam),
                                        teamScores[CtfMode.RedTeam], mode.GetTeamColor(CtfMode.RedTeam));
            }
            SetInterfaceActive(isVisible);
        }

        private void RenderLocalPlayer(SessionBase session, Container sessionContainer, CtfComponent ctf)
        {
            bool isTaking = false, hasFlag = false, isRespawnVisible = false;
            if (session.IsValidLocalPlayer(sessionContainer, out Container localPlayer, false))
            {
                if (localPlayer.Require<HealthProperty>().IsAlive)
                {
                    ArrayElement<FlagArrayElement> flags = ctf.teamFlags;
                    for (var flagTeam = 0; flagTeam < flags.Length; flagTeam++)
                    {
                        if (flagTeam == localPlayer.Require<TeamProperty>()) continue; // Can't possibly take a team flag... Unless???
                        foreach (FlagComponent flag in flags[flagTeam])
                        {
                            if (flag.capturingPlayerId == sessionContainer.Require<LocalPlayerId>())
                            {
                                if (flag.captureElapsedTimeUs < CtfMode.TakeFlagDurationUs)
                                {
                                    isTaking = true;
                                    m_TakeProgress.Set(flag.captureElapsedTimeUs, CtfMode.TakeFlagDurationUs);
                                }
                                else
                                {
                                    hasFlag = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    m_LoadOutInterface.Render(localPlayer.Require<InventoryComponent>());
                    isRespawnVisible = true;
                    var respawn = localPlayer.Require<RespawnTimerProperty>();
                    if (respawn == 0u)
                        m_RespawnText.StartBuild().Append("Press [Enter] to respawn or [B] to change load out").Commit(m_RespawnText);
                    else
                        m_RespawnText.StartBuild().Append("Time until respawn: ").AppendTime(respawn).Commit(m_RespawnText);
                }
            }
            else m_LoadOutInterface.SetInterfaceActive(false);
            m_HasFlagSprite.enabled = hasFlag;
            m_TakeProgress.SetInterfaceActive(isTaking);
            m_RespawnText.enabled = isRespawnVisible;
        }
    }
}