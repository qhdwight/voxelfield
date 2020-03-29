using System;

namespace Components
{
    public class OptionalProperty<T> : Property<T> where T : struct
    {
        private bool m_HasValue;

        public static implicit operator OptionalProperty<T>(T property)
        {
            return new OptionalProperty<T> {m_Value = property, m_HasValue = true};
        }

        public static implicit operator T(OptionalProperty<T> property)
        {
            return property.m_Value;
        }

        public OptionalProperty<T> IfPresent(Action<T> action)
        {
            if (m_HasValue) action(m_Value);
            return this;
        }

        public T OrElse(T @default)
        {
            return m_HasValue ? m_Value : @default;
        }

        public bool HasValue => m_HasValue;
    }
}