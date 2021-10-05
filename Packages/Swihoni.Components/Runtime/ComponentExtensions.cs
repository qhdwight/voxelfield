using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using LiteNetLib.Utils;
using Swihoni.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Swihoni.Components
{
    public static class ComponentExtensions
    {
        public static Vector3 GetVector3(this NetDataReader reader)
            => new(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());

        public static void Put(this NetDataWriter writer, in Vector3 vector)
        {
            writer.Put(vector.x);
            writer.Put(vector.y);
            writer.Put(vector.z);
        }

        public static Quaternion GetQuaternion(this NetDataReader reader)
            => new(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());

        public static void Put(this NetDataWriter writer, in Quaternion quaternion)
        {
            writer.Put(quaternion.x);
            writer.Put(quaternion.y);
            writer.Put(quaternion.z);
            writer.Put(quaternion.w);
        }

        public static void PutColor32(this NetDataWriter writer, in Color32 color)
        {
            writer.Put(color.r);
            writer.Put(color.g);
            writer.Put(color.b);
            writer.Put(color.a);
        }

        public static Color32 GetColor32(this NetDataReader reader)
            => new(reader.GetByte(), reader.GetByte(), reader.GetByte(), reader.GetByte());

        public static void PutColor(this NetDataWriter writer, in Color color)
        {
            writer.Put(color.r);
            writer.Put(color.g);
            writer.Put(color.b);
            writer.Put(color.a);
        }

        public static Color GetColor(this NetDataReader reader)
            => new(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());

        public static bool SameAs(this in Color32 color, in Color32 other) => color.a == other.a && color.g == other.g && color.b == other.b && color.a == other.a;

        public static string Expand(this string @string)
        {
            string expanded = Regex.Replace(@string, "[mM]", "000000");
            return Regex.Replace(expanded, "[kK]", "000");
        }

        public static string ToSnakeCase(this string @string)
            => string.Concat(@string.Select((c, i) => i > 0 && (char.IsUpper(c) || char.IsNumber(c)) ? $"_{c}" : $"{c}")).ToLower();

        public static string ToDisplayCase(this string @string)
            => string.Concat(@string.Select((c, i) => i > 0 && (char.IsUpper(c) || char.IsNumber(c)) ? $" {c}" : $"{c}"));

        public static DualDictionary<T, string> GetNameMap<T>(this Type type) => GetNameMap<T>(type, ToSnakeCase);

        public static DualDictionary<T, string> GetNameMap<T>(this Type type, Func<string, string> func)
            => new(type.IsEnum
                       ? Enum.GetValues(type).OfType<T>().Distinct()
                             .ToDictionary(@enum => @enum,
                                           @enum => func(@enum.ToString()))
                       : type.GetFields(BindingFlags.Static | BindingFlags.Public)
                             .ToDictionary(field => (T) field.GetValue(null),
                                           field => func(field.Name)));

        public static TElement NewElement<TElement>() => (TElement) (object) typeof(TElement).NewElement();

        public static ElementBase NewElement(this Type type, FieldInfo field = null)
        {
            var element = (ElementBase) Activator.CreateInstance(type);
            element.Field = field;
            if (element is ComponentBase component) component.VerifyFieldsRegistered();
            return element;
        }
    }
}