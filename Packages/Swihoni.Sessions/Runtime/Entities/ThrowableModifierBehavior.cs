using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableModifierBehavior : EntityModifierBehavior
    {
        private Rigidbody m_Rigidbody;

        public Rigidbody Rigidbody => m_Rigidbody;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }
    }
}