using System;
using Swihoni.Components;
using Swihoni.Sessions;

namespace Compound.Session
{
    [Serializable]
    public class VoxelMapNameProperty : StringProperty
    {
        public VoxelMapNameProperty() : base(16) { }
    }

    public static class CompoundComponents
    {
        public static readonly SessionElements SessionElements;

        static CompoundComponents()
        {
            SessionElements = SessionElements.NewStandardSessionElements();
            SessionElements.elements.Add(typeof(VoxelMapNameProperty));
        }
    }
}