using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Swihoni.Util
{
    public static class Extensions
    {
        private static readonly Comparer<RaycastHit> DistanceComparer = Comparer<RaycastHit>.Create((h1, h2) => h1.distance.CompareTo(h2.distance));

        public static RaycastHit[] SortByDistance(this RaycastHit[] hits, int count)
        {
            Array.Sort(hits, 0, count, DistanceComparer);
            return hits;
        }

        public static RaycastHit GetClosest(this RaycastHit[] hits, int count) => hits.SortByDistance(count).First();

        public static bool TryClosest(this RaycastHit[] hits, int count, out RaycastHit hit)
        {
            if (count == 0)
            {
                hit = default;
                return false;
            }
            hits.SortByDistance(count);
            hit = hits.First();
            return true;
        }
    }
}