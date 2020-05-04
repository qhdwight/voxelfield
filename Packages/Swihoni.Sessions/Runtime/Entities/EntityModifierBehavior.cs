using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityModifierBehavior : MonoBehaviour
    {
        public byte id;

        public void SetActive(bool isEnabled) => gameObject.SetActive(isEnabled);

        public virtual void Modify(EntityContainer entity) { }
    }
}