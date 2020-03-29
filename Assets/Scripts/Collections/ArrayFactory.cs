using System;
using System.Linq;

namespace Collections
{
    public static class ArrayFactory
    {
        public static T[] Repeat<T>(Func<T> constructor, int size)
        {
            return Enumerable.Range(1, size).Select(_ => constructor()).ToArray();
        }
    }
}