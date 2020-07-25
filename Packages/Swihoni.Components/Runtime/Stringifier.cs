using System.Text;

namespace Swihoni.Components
{
    public static class Stringifier
    {
        private static StringBuilder _builder;

        public static StringBuilder Stringify(this StringBuilder builder, ElementBase element)
        {
            _builder = builder;
            element.NavigateProperties(_property => _property.AppendValue(_builder));
            return _builder;
        }
        
        public static ElementBase Parse(this ElementBase element, string @string)
        {
            // TODO:refactor parsing more than just one property
            if (element is PropertyBase property) property.ParseValue(@string);
            return element;
        }
        
        public static StringBuilder AppendProperty(this StringBuilder builder, PropertyBase property)
            => property.WithValue ? property.AppendValue(builder) : builder.Append("None");

        public static StringBuilder AppendPropertyValue(this StringBuilder builder, PropertyBase property) => property.AppendValue(builder);
    }
}