using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityModifierBehavior : MonoBehaviour
    {
        public int id;
        
        public void SetActive(bool isEnabled) => gameObject.SetActive(isEnabled);
    }
}