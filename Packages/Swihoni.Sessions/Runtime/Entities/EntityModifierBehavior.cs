using Swihoni.Components;

namespace Swihoni.Sessions.Entities
{
    public class EntityModifierBehavior : ModifierBehaviorBase
    {
        public virtual void Modify(SessionBase session, Container container, uint timeUs, uint durationUs) { }
    }
}