using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEditor;
using UnityEngine;

namespace Voxelfield.Editor
{
    [CustomEditor(typeof(ModelBehavior), true)]
    public class ModelBehaviorEditor : UnityEditor.Editor
    {
        private int? m_Team, m_ModeId;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (target is ModelBehavior model)
            {
                if (model.Container.With(out TeamProperty teamProperty))
                    m_Team = EditorGUILayout.IntField("Team", m_Team.GetValueOrDefault(teamProperty.Value));
                if (model.Container.With(out ModeIdProperty modeIdProperty))
                    m_ModeId = EditorGUILayout.IntField("Mode", m_ModeId.GetValueOrDefault(modeIdProperty.Value));
                if (GUILayout.Button("Apply"))
                {
                    teamProperty.Value = (byte) m_Team;
                    modeIdProperty.Value = (byte) m_ModeId;
                }
            }
        }
    }
}