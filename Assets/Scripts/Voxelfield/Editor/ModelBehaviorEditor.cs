using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEditor;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield.Editor
{
    [CustomEditor(typeof(ModelBehavior), true)]
    public class ModelBehaviorEditor : UnityEditor.Editor
    {
        private int? m_Team, m_ModeId;
        private Vector3? m_Extents;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (target is ModelBehavior model && !ReferenceEquals(model.Container, null))
            {
                if (model.Container.With(out TeamProperty teamProperty))
                    m_Team = EditorGUILayout.IntField("Team", m_Team.GetValueOrDefault(teamProperty.Value));
                if (model.Container.With(out ModeIdProperty modeIdProperty))
                    m_ModeId = EditorGUILayout.IntField("Mode", m_ModeId.GetValueOrDefault(modeIdProperty.Value));
                if (model.Container.With(out ExtentsProperty extentsProperty))
                    m_Extents = EditorGUILayout.Vector3Field("Extents", m_Extents.GetValueOrDefault(extentsProperty.Value));
                if (GUILayout.Button("Apply"))
                {
                    if (m_Team is int team) teamProperty.Value = (byte) team;
                    if (m_ModeId is int modeId) modeIdProperty.Value = (byte) modeId;
                    if (m_Extents is Vector3 extents) extentsProperty.Value = extents;
                }
            }
        }
    }
}