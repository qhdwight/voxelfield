using System;
using UnityEngine;

namespace Swihoni.Util.Animation
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class ChildBehavior : MonoBehaviour
    {
        [Serializable]
        private class Parent
        {
            public Transform transform = default;
            public Vector3 positionOffset = default, rotationOffset = default;
            public float weight = 1.0f;
        }

        [SerializeField] private Parent[] m_Parents = default;

        public void Evaluate()
        {
            Vector3 position = Vector3.zero, rotation = Vector3.zero;
            foreach (Parent parent in m_Parents)
            {
                position += parent.transform.TransformPoint(parent.positionOffset);
                rotation += parent.rotationOffset;
            }
            Transform t = transform;
            t.position = position / m_Parents.Length;
            t.localEulerAngles = rotation / m_Parents.Length;
        }

        private void LateUpdate()
        {
            if (Application.isPlaying) return;
            Evaluate();
        }
    }
}