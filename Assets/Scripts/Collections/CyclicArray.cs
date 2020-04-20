using System;
using System.Collections;
using System.Collections.Generic;

namespace Collections
{
    /// <summary>
    /// An array that loops back to the first element and overwrites existing ones when the end is reached.
    /// <see cref="m_Pointer"/> refers to the current "starting" index in the array.
    /// </summary>
    /// <typeparam name="TElement">Type of element to store</typeparam>
    public class CyclicArray<TElement> : IEnumerable<TElement>
    {
        public delegate bool GetPredicate(in TElement element);

        protected readonly TElement[] m_InternalArray;
        protected int m_Pointer;
        protected readonly int m_Size;

        public TElement[] InternalArray => m_InternalArray;
        public int Pointer => m_Pointer;
        public int Size => m_Size;

        /// <summary>
        /// Create a new cyclic array with the given size and optional default value for each element.
        /// </summary>
        /// <param name="size">Size of the internal array</param>
        /// <param name="constructor">Default value for each element in the array</param>
        public CyclicArray(int size, Func<TElement> constructor = null)
        {
            m_Pointer = m_Size - 1; // This makes it so that the first item added goes to index zero
            m_Size = size;
            m_InternalArray = new TElement[size];
            for (var i = 0; i < size; i++)
                m_InternalArray[i] = constructor == null ? default : constructor();
        }

        /// <summary>
        /// Increase the pointer by one
        /// </summary>
        public void Advance()
        {
            m_Pointer = Wrap(m_Pointer + 1);
        }

        public TElement ClaimNext()
        {
            Advance();
            return Peek();
        }

        /// <summary>
        /// Add an element next to the current element defined by the pointer and update the pointer to the given element.
        /// </summary>
        /// <param name="item">Element to add</param>
        public virtual void Add(in TElement item)
        {
            Advance();
            m_InternalArray[m_Pointer] = item;
        }

        /// <summary>
        /// Take a relative offset to the current pointer and return the absolute index into the internal array.
        /// </summary>
        /// <param name="relativeOffset">Relative offset to current pointer</param>
        /// <returns>Absolute index to internal array</returns>
        private int GetAbsoluteFromRelativeOffset(int relativeOffset)
        {
            return Wrap(m_Pointer + relativeOffset);
        }

        /// <summary>
        /// Get an element relative to the current pointer.
        /// Zero index or offset would result in the current element being pointed to.
        /// </summary>
        /// <param name="offset">Relative offset to element from pointer</param>
        /// <returns>Element at the relative offset from the pointer</returns>
        public ref TElement Get(int offset)
        {
            // TODO safety handle the case where offset is bigger than size?
            return ref m_InternalArray[GetAbsoluteFromRelativeOffset(offset)];
        }

        public ref TElement GetWithPredicate(GetPredicate predicate)
        {
            for (var offset = 0; offset > -m_Size; offset--)
            {
                ref TElement element = ref Get(offset);
                if (!predicate(element)) continue;
                return ref element;
            }
            return ref Peek();
        }

        /// <summary>
        /// Set an element relative to the current pointer.
        /// </summary>
        /// <param name="offset">Relative offset to element from pointer</param>
        /// <param name="newElement">New element to put at the given offset</param>
        public void Set(int offset, in TElement newElement)
        {
            m_InternalArray[GetAbsoluteFromRelativeOffset(offset)] = newElement;
        }

        /// <summary>
        /// Given the size, a chunk of elements can be imagined including the current one pointed at by the current pointer.
        /// This chunk extends backwards in index, thus it is in "history."
        /// Given the supplied index, find the element in that chunk.
        /// A zero index would be the first element in the chunk, that with the lowest index in the internal array.
        /// </summary>
        /// <param name="size">Size of the chunk</param>
        /// <param name="index">Index in the chunk</param>
        /// <returns></returns>
        public TElement GetInHistoryChunk(int size, int index)
        {
            return Get(index - size + 1);
        }

        /// <summary>
        /// Negative indices or ones that are the internal arrays size or more need to be wraped around
        /// to properly index the internal array. This helper function finds that desired valid index.
        /// </summary>
        /// <param name="index">Index to wrap into internal array index</param>
        /// <returns>Valid index into internal array</returns>
        private int Wrap(int index)
        {
            int remainder = index % m_Size;
            return remainder < 0 ? remainder + m_Size : remainder;
        }

        /// <summary>
        /// Get the element currently reference by the pointer
        /// </summary>
        /// <returns>Element at index of current pointer</returns>
        public ref TElement Peek()
        {
            return ref Get(0);
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return ((IEnumerable<TElement>) m_InternalArray).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_InternalArray.GetEnumerator();
        }
    }
}