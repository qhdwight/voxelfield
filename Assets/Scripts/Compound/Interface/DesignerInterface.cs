using System.Text;
using Compound.Session;
using Compound.Session.Mode;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Compound.Interface
{
    public class DesignerInterface : SessionInterfaceBehavior
    {
        [SerializeField] private BufferedTextGui m_InformationText = default;

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = session.GetMode(sessionContainer) is DesignerMode
                          && sessionContainer.Require<LocalPlayerId>().WithValue;
            if (isVisible)
            {
                int localPlayerId = sessionContainer.Require<LocalPlayerId>();
                Container localPlayer = session.GetPlayerFromId(localPlayerId, sessionContainer);
                var designer = localPlayer.Require<DesignerPlayerComponent>();
                void Build(StringBuilder builder)
                {
                    AppendProperty("P1: ", designer.positionOne, builder).Append("\n");
                    AppendProperty("P2: ", designer.positionTwo, builder).Append("\n");
                    AppendProperty("Selected: ", designer.selectedBlockId, builder);
                }
                m_InformationText.BuildText(Build);
            }
            SetInterfaceActive(isVisible);
        }

        private static StringBuilder AppendProperty<T>(string prefix, PropertyBase<T> position, StringBuilder builder) where T : struct
        {
            builder.Append(prefix);
            if (position.WithValue) builder.Append(position.Value);
            else builder.Append("Not set");
            return builder;
        }
    }
}