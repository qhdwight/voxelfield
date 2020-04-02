using System.Collections.Generic;

namespace Util
{
    public static class Extensions
    {
        public static Dictionary<TEnum, TValue> ToEnumDictionary<TEnum, TValue>(this TValue[] array)
        {
            var dictionary = new Dictionary<TEnum, TValue>(array.Length);
            for (var i = 0; i < array.Length; i++)
            {
                dictionary.Add((TEnum) (object) i, array[i]);
            }
            return dictionary;
        }
    }
}