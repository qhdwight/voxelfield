using System.Text;
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
        [SerializeField] private AudioSource m_TakeFlagAudioSource = default;
        [SerializeField] private BufferedTextGui m_NotificationText = default;
        private LoadOutInterface m_LoadOutInterface;
        private CtfMode m_CtfMode;
        private readonly uint?[] m_LastFlagCaptureTimesUs = new uint?[FlagArrayElement.Count * 2];

        protected override void Awake()
        {
            base.Awake();
            m_LoadOutInterface = GetComponentInChildren<LoadOutInterface>();
            m_CtfMode = (CtfMode) ModeManager.GetMode(ModeIdProperty.Ctf);
        }

        public override void Render(in SessionContext context)
        {
            Container sessionContainer = context.sessionContainer;
            bool isVisible = sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Ctf;
            if (isVisible)
            {
                var ctf = sessionContainer.Require<CtfComponent>();
                RenderLocalPlayer(context, ctf);

                var teamScores = sessionContainer.Require<DualScoresComponent>();
                m_ScoreInterface.Render(teamScores[CtfMode.BlueTeam], m_CtfMode.GetTeamColor(CtfMode.BlueTeam),
                                        teamScores[CtfMode.RedTeam], m_CtfMode.GetTeamColor(CtfMode.RedTeam));
            }
            SetInterfaceActive(isVisible);
        }

        private void RenderLocalPlayer(in SessionContext context, CtfComponent ctf)
        {
            bool localTaking = false, localHasFlag = false, isRespawnVisible = false, isNotificationVisible = false;
            if (context.IsValidLocalPlayer(out Container localPlayer, out byte localPlayerId, false))
            {
                if (localPlayer.Require<HealthProperty>().IsAlive)
                {
                    ArrayElement<FlagArrayElement> flags = ctf.teamFlags;
                    for (var flagTeam = 0; flagTeam < flags.Length; flagTeam++)
                    {
                        for (var flagIndex = 0; flagIndex < flags[flagTeam].Length; flagIndex++)
                        {
                            FlagComponent flag = flags[flagTeam][flagIndex];
                            ElapsedUsProperty captureTimeUs = flag.captureElapsedTimeUs;
                            if (flag.capturingPlayerId.TryWithValue(out byte capturingPlayerId))
                            {
                                bool isTaking = captureTimeUs < CtfMode.TakeFlagDurationUs;
                                if (capturingPlayerId == localPlayerId)
                                {
                                    if (isTaking)
                                    {
                                        localTaking = true;
                                        m_TakeProgress.Set(captureTimeUs, CtfMode.TakeFlagDurationUs);
                                    }
                                    else localHasFlag = true;
                                }
                                isNotificationVisible = true;
                                StringBuilder builder = m_NotificationText.StartBuild();
                                m_CtfMode.AppendUsername(builder, context.GetPlayer(capturingPlayerId))
                                         .Append(isTaking ? " is taking a flag" : " has a flag")
                                         .Commit(m_NotificationText);
                            }
                            int flatIndex = flagTeam * flags.Length + flagIndex;
                            if (m_LastFlagCaptureTimesUs[flatIndex] is uint lt && captureTimeUs.TryWithValue(out uint ct)
                                                                               && lt < CtfMode.TakeFlagDurationUs && ct > CtfMode.TakeFlagDurationUs)
                                m_TakeFlagAudioSource.Play();
                            m_LastFlagCaptureTimesUs[flatIndex] = captureTimeUs.AsNullable;
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
            m_HasFlagSprite.enabled = localHasFlag;
            m_TakeProgress.SetInterfaceActive(localTaking);
            m_RespawnText.enabled = isRespawnVisible;
            m_NotificationText.enabled = isNotificationVisible;
        }
    }
}