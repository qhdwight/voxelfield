using System;
using UnityEngine;

namespace Swihoni.Util.Animation
{

    public class ChildBehavior : MonoBehaviour
    {
        [Serializable]
        private class Parent
        {
            public Transform transform = default;
            public Vector3 offset = default;
            public float weight = 1.0f;
        }

        [SerializeField] private Parent[] m_Parents;

        public void Evaluate()
        {
            
        }

        private void LateUpdate()
        {
            Evaluate();
        }
    }
}