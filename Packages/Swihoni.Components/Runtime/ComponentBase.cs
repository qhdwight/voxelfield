using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Swihoni.Components
{
    public class SingleTickAttribute : Attribute
    {
        public bool Zero { get; }

        public SingleTickAttribute(bool zero = false) => Zero = zero;
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
        
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() => Field != null ? Field.GetHashCode() : 0;

        public bool WithAttribute<T>() where T : Attribute => TryAttribute(out T _);

        public bool WithoutAttribute<T>() where T : Attribute => !WithAttribute<T>();

        public bool TryAttribute<T>(out T attribute) where T : Attribute
            => ReflectionCache.TryAttribute(GetType(), out attribute) || Field != null && ReflectionCache.TryAttribute(Field, out attribute);
    }

    /// <summary>
    /// Stores a collection of elements. Access via <see cref="Elements"/> or with indexer.
    /// Once you add an element, you need to make sure it is "owned" by the component (effectively moving it).
    /// A conscious choice was made to use classes instead of structs, so proper responsibility has to be taken.
    /// What this means in practice is, unless you know exactly what you are doing, once you <see cref="Append"/>
    /// </summary>
    public abstract class ComponentBase : ElementBase, IEnumerable<ElementBase>
    {
        public List<ElementBase> m_Elements;
        
        public IReadOnlyList<ElementBase> Elements
        {
            get
            {
                VerifyFieldsRegistered();
                return m_Elements;
            }
        }

        public ElementBase this[int index] => Elements[index];

        public int Count => Elements.Count;

        internal void VerifyFieldsRegistered()
        {
            if (m_Elements != null) return;

            IReadOnlyList<FieldInfo> fieldInfos = ReflectionCache.GetFieldInfo(GetType());
            m_Elements = new List<ElementBase>(fieldInfos.Count);
            foreach (FieldInfo field in fieldInfos)
            {
                object fieldValue = field.GetValue(this);
                if (fieldValue is ElementBase element) Append(element);
            }
        }

        protected ComponentBase() => InstantiateFieldElements();

        private void InstantiateFieldElements()
        {
            foreach (FieldInfo field in ReflectionCache.GetFieldInfo(GetType()))
            {
                Type fieldType = field.FieldType;
                bool isElement = fieldType.IsElement();
                if (!isElement || fieldType.IsAbstract || field.GetValue(this) != null) continue;

                ElementBase element = fieldType.NewElement(field);
                field.SetValue(this, element);
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
        public virtual void CustomInterpolateFrom(ComponentBase c1, ComponentBase c2, float interpolation) { }

        public IEnumerator<ElementBase> GetEnumerator() => Elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}