using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Player.Components;
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
        
        public override void Render(SessionBase session, Container sessionContainer)
        {
            // m_TakeProgress.Set();
            var ctf = sessionContainer.Require<CtfComponent>();
            RenderLocalPlayer(session, sessionContainer, ctf);
            m_ScoreInterface.Render(ctf.teamScores[CtfMode.BlueTeam], Color.blue, ctf.teamScores[CtfMode.RedTeam], Color.red);
        }

        private void RenderLocalPlayer(SessionBase session, Container sessionContainer, CtfComponent ctf)
        {
            bool isTaking = false, hasFlag = false;
            if (ShowdownInterface.IsValidLocalPlayer(session, sessionContainer, out Container localPlayer))
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
            m_HasFlagSprite.enabled = hasFlag;
            m_TakeProgress.SetInterfaceActive(isTaking);
        }
    }
}