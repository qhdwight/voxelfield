using System;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using Voxel;
using Voxel.Map;

namespace Voxelfield.Session
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

    [Serializable]
    public class MoneyComponent : ComponentBase
    {
        [OnlyServerTrusted] public UShortProperty count;
        [ClientTrusted] public ByteProperty wantedBuyItemId;
    }

    public static class VoxelfieldComponents
    {
        [RuntimeInitializeOnLoadMethod]
        public static void Initialize() => SerializationRegistrar.RegisterAll(typeof(ModelIdProperty), typeof(TeamProperty), typeof(VectorProperty));

        public static readonly SessionElements SessionElements;

        static VoxelfieldComponents()
        {
            SessionElements = SessionElements.NewStandardSessionElements();
            SessionElements.playerElements.AppendAll(typeof(ShowdownPlayerComponent), typeof(DesignerPlayerComponent), typeof(MoneyComponent));
            // SessionElements.commandElements.AppendAll(typeof(TeamProperty));
            SessionElements.elements.AppendAll(typeof(VoxelMapNameProperty), typeof(ChangedVoxelsProperty), typeof(ShowdownSessionComponent));
        }
    }
}