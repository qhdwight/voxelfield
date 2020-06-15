using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityModifierBehavior : MonoBehaviour
    {
        public byte id;

        public virtual void SetActive(bool isEnabled) => gameObject.SetActive(isEnabled);

        public virtual void Modify(SessionBase session, EntityContainer entity, uint timeUs, uint durationUs) { }
    }
}