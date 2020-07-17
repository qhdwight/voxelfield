using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Voxelfield.Session.Mode
{
    public class SiteBehavior : ModelBehavior
    {
        public Collider Trigger { get; private set; }

        private void Awake() { Trigger = GetComponent<Collider>(); }

        public override void SetInMode(Container session) => gameObject.SetActive(IsModeOrDesigner(session, ModeIdProperty.SecureArea));

        public void Render(SiteComponent site) { }

        public override void RenderContainer()
        {
            Vector3 extents = Container.Require<ExtentsProperty>();
            transform.localScale = extents * 0.99f;
        }
    }
}