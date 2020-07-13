using System;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Voxelfield.Session.Mode
{
    public class SiteBehavior : ModelBehavior
    {
        public Collider Trigger { get; private set; }

        private void Awake()
        {
            Trigger = GetComponent<Collider>();
        }

        public override void SetInMode(Container session) => gameObject.SetActive(IsModeOrDesigner(session, ModeIdProperty.SecureArea));

        public void Render(SiteComponent site) {  }
    }
}