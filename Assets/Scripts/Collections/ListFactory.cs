using System;
using System.Collections.Generic;
using System.Linq;

namespace Collections
{
    public static class ListFactory
    {
        public static List<T> Repeat<T>(Func<T> constructor, int size)
        {
            return Enumerable.Range(1, size).Select(_ => constructor()).ToList();
        }
    }
}