using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Voxelfield.Session.Mode
{
    public class PickUpBehavior : ModelBehavior
    {
        public enum Type
        {
            Health, Ammo
        }

        [SerializeField] private Type m_Type;

        public Type T => m_Type;

        public void Render(bool isActive, uint timeUs)
        {
            gameObject.SetActive(isActive);
            if (isActive) transform.rotation = Quaternion.AngleAxis(Mathf.Repeat(timeUs / 10_000f, 360.0f), Vector3.up);
        }
        
        public override void SetInMode(Container session) => gameObject.SetActive(IsModeOrDesigner(session, ModeIdProperty.Ctf));
    }
}