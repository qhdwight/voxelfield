using System;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace Swihoni.Collections
{
    public class DurationAverage : CyclicArray<double>
    {
        private readonly string m_Tag;
        private readonly Stopwatch m_Stopwatch = new();
        
        public DurationAverage(int size, string tag) : base(size, () => default) => m_Tag = tag;

        public void Start()
        {
            m_Stopwatch.Restart();
        }

        public void Stop(bool log = false)
        {
            m_Stopwatch.Stop();
            Add(m_Stopwatch.Elapsed.TotalMilliseconds);
            if (log) Debug.Log($"[{m_Tag}] {Average():F2} ms");
        }

        public double Average() => m_InternalArray.Average();
    }
}