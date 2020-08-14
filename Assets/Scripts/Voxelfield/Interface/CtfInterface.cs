using System.Text;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
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
        private readonly uint?[] m_LastFlagCaptureTimesUs = new uint?[FlagArray.Count * TeamFlagArray.Count];

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

                var teamScores = sessionContainer.Require<DualScoresArray>();
                m_ScoreInterface.Render(teamScores[CtfMode.BlueTeam], m_CtfMode.GetTeamColor(CtfMode.BlueTeam),
                                        teamScores[CtfMode.RedTeam], m_CtfMode.GetTeamColor(CtfMode.RedTeam));
            }
            SetInterfaceActive(isVisible);
        }

        private void RenderLocalPlayer(in SessionContext context, CtfComponent ctf)
        {
            bool localTaking = false, localHasFlag = false, isRespawnVisible = false;
            StringBuilder notificationBuilder = m_NotificationText.Builder.Clear();

            bool isValidLocalPlayer = context.IsValidLocalPlayer(out Container localPlayer, out byte localPlayerId, false);
            if (isValidLocalPlayer)
            {
                if (localPlayer.Health().IsDead)
                {
                    m_LoadOutInterface.Render(localPlayer.Require<InventoryComponent>());
                    isRespawnVisible = true;
                    var respawn = localPlayer.Require<RespawnTimerProperty>();
                    if (respawn == 0u)
                        m_RespawnText.StartBuild().Append("Press [").AppendInputKey(PlayerInput.Respawn)
                                     .Append("] to respawn or [")
                                     .AppendInputKey(InputType.Buy).Append("] to change load out").Commit(m_RespawnText);
                    else
                        m_RespawnText.StartBuild().Append("Time until respawn: ").AppendTime(respawn).Commit(m_RespawnText);
                }
            }
            else m_LoadOutInterface.SetInterfaceActive(false);

            TeamFlagArray flags = ctf.teamFlags;
            for (var flagTeam = 0; flagTeam < flags.Length; flagTeam++)
            {
                for (var flagIndex = 0; flagIndex < flags[flagTeam].Length; flagIndex++)
                {
                    FlagComponent flag = flags[flagTeam][flagIndex];
                    ElapsedUsProperty captureTimeUs = flag.captureElapsedTimeUs;
                    if (flag.capturingPlayerId.TryWithValue(out byte capturingPlayerId))
                    {
                        bool isTaking = captureTimeUs < CtfMode.TakeFlagDurationUs;
                        if (isValidLocalPlayer && capturingPlayerId == localPlayerId)
                        {
                            if (isTaking)
                            {
                                localTaking = true;
                                m_TakeProgress.Set(captureTimeUs, CtfMode.TakeFlagDurationUs);
                            }
                            else localHasFlag = true;
                        }
                        if (notificationBuilder.Length > 0) notificationBuilder.Append("\n");
                        m_CtfMode.AppendUsername(notificationBuilder, context.GetPlayer(capturingPlayerId))
                                 .Append(isTaking ? " is taking a flag" : " has a flag");
                    }
                    int flatIndex = flagTeam * flags.Length + flagIndex;
                    if (m_LastFlagCaptureTimesUs[flatIndex] is uint lt && captureTimeUs.TryWithValue(out uint ct)
                                                                       && lt < CtfMode.TakeFlagDurationUs && ct > CtfMode.TakeFlagDurationUs)
                        m_TakeFlagAudioSource.Play();
                    m_LastFlagCaptureTimesUs[flatIndex] = captureTimeUs.AsNullable;
                }
            }

            m_HasFlagSprite.enabled = localHasFlag;
            m_TakeProgress.SetInterfaceActive(localTaking);
            m_RespawnText.enabled = isRespawnVisible;
            m_NotificationText.SetText(notificationBuilder);
        }
    }
}