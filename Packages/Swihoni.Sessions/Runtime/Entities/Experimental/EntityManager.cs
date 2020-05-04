// using System;
// using Swihoni.Collections;
// using Swihoni.Components;
// using Swihoni.Sessions.Mapper;
// using UnityEngine;
// using UnityObject = UnityEngine.Object;
//
// namespace Swihoni.Sessions.Entities
// {
//     public abstract class EntityVisuals : MeshedElementVisualsBase<EntityContainer>, IIdentifiable
//     {
//         [SerializeField] protected int m_Id;
//
//         public int GetId() => m_Id;
//     }
//
//     public abstract class EntityModifier : MonoBehaviour, IElementModifier<EntityContainer, Container>, IIdentifiable
//     {
//         [SerializeField] private int m_Id;
//
//         public virtual void Dispose() => Destroy(gameObject);
//
//         public virtual void Setup(SessionBase session) { throw new NotImplementedException(); }
//
//         public virtual void Modify(SessionBase session, Container root, int index, EntityContainer element, Container commands, float duration) { throw new NotImplementedException(); }
//
//         public virtual void Synchronize(EntityContainer element) { throw new NotImplementedException(); }
//
//         public virtual void ModifyCommands(SessionBase session, Container commands) { throw new NotImplementedException(); }
//
//         public virtual void SetActive(bool isActive) { throw new NotImplementedException(); }
//
//         public virtual int GetId() { throw new NotImplementedException(); }
//     }
//
//     public class EntityGameObjectMapper : UnityObjectMapperModifier<EntityContainer, Container>
//     {
//         private Pool<EntityVisuals>[] m_VisualsPool;
//         private Pool<EntityModifier>[] m_ModifiersPool;
//
//         public EntityGameObjectMapper()
//         {
//             m_VisualsPool = LoadComponentPoolFromResource<EntityVisuals>("Entities");
//             m_ModifiersPool = LoadComponentPoolFromResource<EntityModifier>("Entities");
//         }
//
//         protected override IElementModifier<EntityContainer, Container> SynchronizeModifier(int index, EntityContainer element)
//         {
//             byte id = element.Require<EntityId>();
//             switch (id)
//             {
//                 case EntityId.None:
//                     return null;
//                 default:
//                     return m_ModifiersPool[id].Obtain();
//             }
//         }
//
//         protected override void SwitchModifier(IElementModifier<EntityContainer, Container> oldModifier, IElementModifier<EntityContainer, Container> newModifier) { base.SwitchModifier(oldModifier, newModifier); }
//
//         protected override void SwitchVisuals(IElementVisuals<EntityContainer> oldVisuals, IElementVisuals<EntityContainer> newVisuals) { base.SwitchVisuals(oldVisuals, newVisuals); }
//
//         protected override IElementVisuals<EntityContainer> SynchronizeVisuals(int index, EntityContainer element) { throw new NotImplementedException(); }
//     }
// }

