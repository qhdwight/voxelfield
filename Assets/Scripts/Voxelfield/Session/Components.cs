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

    [Serializable]
    public class VoxelMapNameProperty : StringProperty
    {
        public VoxelMapNameProperty() : base(16) { }
    }
    
    [Serializable]
    public class ShowdownSessionComponent : ComponentBase
    {
        public ByteProperty number;
        public UIntTimeProperty remainingUs;
        public ArrayElement<CurePackageComponent> curePackages = new ArrayElement<CurePackageComponent>(9);
    }

    /* Player */

    [Serializable]
    public class CurePackageComponent : ComponentBase
    {
        public BoolProperty isActive;
    }
    
    [Serializable, OnlyServerTrusted]
    public class ShowdownPlayerComponent : ComponentBase
    {
        public ByteProperty stagesCuredFlags;
        public UIntTimeProperty elapsedSecuringUs;

        public bool IsCured(ShowdownSessionComponent showdown) => (stagesCuredFlags & (byte) (1 << showdown.number)) != 0;
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
        [ClientTrusted, SingleTick] public ByteProperty wantedBuyItemId;
    }

    public static class VoxelfieldComponents
    {
        [RuntimeInitializeOnLoadMethod]
        public static void Initialize() => SerializationRegistrar.RegisterAll(typeof(ModelIdProperty), typeof(IdProperty), typeof(TeamProperty), typeof(VectorProperty));

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