using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Swihoni.Components
{
    public class SingleTick : Attribute
    {
    }

    public abstract class ElementBase
    {
        public FieldInfo Field { get; set; }

        private bool Equals(ElementBase other) { return this.EqualTo(other); }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.GetType() == GetType() && Equals((ElementBase) other);
        }

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        public bool WithAttribute<T>() => GetType().IsDefined(typeof(T)) || Field != null && Field.IsDefined(typeof(T));

        public bool TryAttribute<T>(out T attribute) where T : Attribute
        {
            if (WithAttribute<T>())
            {
                attribute = Field.GetCustomAttribute<T>();
                return true;
            }
            attribute = null;
            return false;
        }
    }

    /// <summary>
    /// Stores a collection of elements. Access via <see cref="Elements"/> or with indexer.
    /// Once you add an element, you need to make sure it is "owned" by the component (effectively moving it).
    /// A conscious choice was made to use classes instead of structs, so proper responsibility has to be taken.
    /// What this means in practice is, unless you know exactly what you are doing, once you <see cref="Append"/>
    /// </summary>
    public abstract class ComponentBase : ElementBase, IEnumerable<ElementBase>
    {
        private List<ElementBase> m_Elements;

        public IReadOnlyList<ElementBase> Elements
        {
            get
            {
                VerifyFieldsRegistered();
                return m_Elements;
            }
        }

        public ElementBase this[int index] => Elements[index];

        protected void VerifyFieldsRegistered()
        {
            if (m_Elements != null) return;

            m_Elements = new List<ElementBase>();
            IReadOnlyList<FieldInfo> fieldInfos = ReflectionCache.GetFieldInfo(GetType());
            foreach (FieldInfo field in fieldInfos)
            {
                object fieldValue = field.GetValue(this);
                if (fieldValue is ElementBase element) Append(element);
            }
        }

        protected void ClearRegistered() => m_Elements = new List<ElementBase>();

        protected ComponentBase() => InstantiateFieldElements();

        private void InstantiateFieldElements()
        {
            foreach (FieldInfo field in ReflectionCache.GetFieldInfo(GetType()))
            {
                Type fieldType = field.FieldType;
                bool isElement = fieldType.IsElement();
                if (!isElement || fieldType.IsAbstract || field.GetValue(this) != null) continue;

                object instance = Activator.CreateInstance(fieldType);
                if (instance is ElementBase elementInstance) elementInstance.Field = field;
                field.SetValue(this, instance);
            }
        }

        /// <summary>
        /// Appends an element to the end of this component.
        /// To be able to retrieve by type, consider using a <see cref="Container"/>.
        /// For registering an element with a component, you wil need to remember the index.
        /// </summary>
        /// <returns>Index of element</returns>
        public virtual int Append(ElementBase element)
        {
            VerifyFieldsRegistered();
            m_Elements.Add(element);
            return m_Elements.Count - 1;
        }

        /// <summary>
        /// Called during interpolation. Use to add custom behavior.
        /// </summary>
        public virtual void InterpolateFrom(ComponentBase c1, ComponentBase c2, float interpolation) { }

        public IEnumerator<ElementBase> GetEnumerator() => m_Elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}