using System;
using UnityEngine;

namespace Swihoni.Util.Animation
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class ChildBehavior : MonoBehaviour
    {
        [Serializable]
        private struct Parent
        {
            public Transform transform;
            public Vector3 positionOffset, rotationOffset;
            public float weight;

            public Quaternion QuaternionRotationOffset => Quaternion.Euler(rotationOffset);
        }

        [SerializeField] private Vector3 positionAtRest = default, rotationAtRest = default;
        [SerializeField] private float weight = 1.0f;

        [SerializeField] private Parent m_ParentOne = default, m_ParentTwo = default;

        public Quaternion QuaternionRotationAtRest => Quaternion.Euler(rotationAtRest);

        public void Evaluate()
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            Transform t = transform;
            var denominator = 0.0f;
            {
                Transform parent = t.parent;
                position += parent == null ? positionAtRest : parent.TransformPoint(positionAtRest) * weight;
                rotation *= Quaternion.Lerp(Quaternion.identity, parent.localRotation * QuaternionRotationAtRest, weight);
                denominator += weight;
            }
            void AddParent(in Parent parent)
            {
                if (parent.transform == null) return;
                position += parent.transform.TransformPoint(parent.positionOffset) * parent.weight;
                rotation *= Quaternion.Lerp(Quaternion.identity, parent.transform.localRotation * parent.QuaternionRotationOffset, parent.weight);
                denominator += parent.weight;
            }
            AddParent(m_ParentOne);
            AddParent(m_ParentTwo);
            if (denominator < 1.0f) return;
            t.position = position / denominator;
            t.localRotation = Quaternion.Slerp(Quaternion.identity, rotation, 1.0f / denominator);
        }

#if UNITY_EDITOR
        private void LateUpdate()
        {
            if (Application.isPlaying || GetComponentInParent<Animator>() == null) return;
            Evaluate();
        }
#endif
    }
}