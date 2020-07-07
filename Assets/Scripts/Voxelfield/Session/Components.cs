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
        public TimeUsProperty remainingUs;
        public ArrayElement<CurePackageComponent> curePackages = new ArrayElement<CurePackageComponent>(9);
    }

    [Serializable]
    public class FlagComponent : ComponentBase
    {
        public ByteProperty capturingPlayerId;
        public ElapsedUsProperty captureElapsedTimeUs;
    }

    [Serializable]
    public class FlagArrayElement : ArrayElement<FlagComponent>
    {
        public FlagArrayElement() : base(2) { }
    }

    [Serializable]
    public class CtfComponent : ComponentBase
    {
        public ArrayElement<ByteProperty> teamScores = new ArrayElement<ByteProperty>(2);
        public ArrayElement<FlagArrayElement> teamFlags = new ArrayElement<FlagArrayElement>(2);
    }
    
    [Serializable]
    public class CurePackageComponent : ComponentBase
    {
        public BoolProperty isActive;
    }

    /* Player */

    [Serializable, OnlyServerTrusted]
    public class ShowdownPlayerComponent : ComponentBase
    {
        public ByteProperty stagesCuredFlags;
        public ElapsedUsProperty elapsedSecuringUs;

        public bool IsCured(ShowdownSessionComponent showdown) => (stagesCuredFlags & (byte) (1 << showdown.number)) != 0;
    }

    [Serializable, OnlyServerTrusted]
    public class DesignerPlayerComponent : ComponentBase
    {
        public Position3IntProperty positionOne, positionTwo;
        public ByteProperty selectedBlockId;
        public UShortProperty selectedModelId;
    }

    [Serializable]
    public class MoneyComponent : ComponentBase
    {
        [OnlyServerTrusted] public UShortProperty count;
        [ClientTrusted, SingleTick] public ByteProperty wantedBuyItemId;
    }

    [Serializable, OnlyServerTrusted]
    public class BrokeVoxelTickProperty : ByteProperty
    {
    }

    public static class VoxelfieldComponents
    {
        [RuntimeInitializeOnLoadMethod]
        public static void Initialize() => SerializationRegistrar.RegisterAll(typeof(ModelIdProperty), typeof(IdProperty), typeof(TeamProperty), typeof(VectorProperty), typeof(ModeIdProperty));

        public static readonly SessionElements SessionElements;

        static VoxelfieldComponents()
        {
            SessionElements = SessionElements.NewStandardSessionElements();
            SessionElements.playerElements.AppendAll(typeof(ShowdownPlayerComponent), typeof(DesignerPlayerComponent), typeof(MoneyComponent), typeof(BrokeVoxelTickProperty));
            // SessionElements.commandElements.AppendAll(typeof(TeamProperty));
            SessionElements.elements.AppendAll(typeof(VoxelMapNameProperty), typeof(ChangedVoxelsProperty), typeof(CtfComponent), typeof(ShowdownSessionComponent));
        }
    }
}