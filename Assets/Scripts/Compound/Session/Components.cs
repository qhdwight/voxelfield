using System;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Voxel;

namespace Compound.Session
{
    /* Session */

    [Serializable, OnlyServerTrusted]
    public class VoxelMapNameProperty : StringProperty
    {
        public VoxelMapNameProperty() : base(16) { }
    }
    
    [Serializable, OnlyServerTrusted]
    public class ShowdownSessionComponent : ComponentBase
    {
        public ByteProperty number;
        public UIntProperty remainingUs;
    }

    /* Player */

    [Serializable, OnlyServerTrusted]
    public class ShowdownPlayerComponent : ComponentBase
    {
        public ByteProperty cured;
    }

    [Serializable]
    public class DesignerPlayerComponent : ComponentBase
    {
        public Position3IntProperty positionOne, positionTwo;
        public ByteProperty selectedBlockId;
    }

    [Serializable, OnlyServerTrusted]
    public class MoneyProperty : UShortProperty
    {
    }

    public static class VoxelfieldComponents
    {
        public static readonly SessionElements SessionElements;

        static VoxelfieldComponents()
        {
            SessionElements = SessionElements.NewStandardSessionElements();
            SessionElements.playerElements.AppendAll(typeof(ShowdownPlayerComponent), typeof(DesignerPlayerComponent), typeof(MoneyProperty));
            // SessionElements.commandElements.AppendAll(typeof(TeamProperty));
            SessionElements.elements.AppendAll(typeof(VoxelMapNameProperty), typeof(ChangedVoxelsProperty), typeof(ShowdownSessionComponent));
        }
    }
}