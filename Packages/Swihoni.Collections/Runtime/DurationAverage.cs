using System.Diagnostics;
using System.Linq;

namespace Swihoni.Collections
{
    public class DurationAverage : CyclicArray<double>
    {
        private readonly Stopwatch m_Stopwatch = new Stopwatch();
        
        public DurationAverage(int size) : base(size, () => default)
        {
        }

        public void Start()
        {
            m_Stopwatch.Restart();
        }

        public void Stop()
        {
            m_Stopwatch.Stop();
            Add(m_Stopwatch.Elapsed.TotalMilliseconds);
        }

        public double Average() => m_InternalArray.Average();
    }
}