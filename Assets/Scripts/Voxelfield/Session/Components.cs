using System;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using Voxels;
using Voxels.Map;

namespace Voxelfield.Session
{
    /* Session */

    [Serializable]
    public class VoxelMapNameProperty : StringProperty
    {
        public VoxelMapNameProperty(string @string) : base(@string, 16) { }
        public VoxelMapNameProperty() : base(16) { }
    }

    [Serializable, ModeElement]
    public class ShowdownSessionComponent : ComponentBase
    {
        public ByteProperty number;
        public TimeUsProperty remainingUs;
        public ArrayElement<CurePackageComponent> curePackages = new ArrayElement<CurePackageComponent>(9);
    }

    [Serializable, ModeElement]
    public class DualScoresComponent : ArrayElement<ByteProperty>
    {
        public DualScoresComponent() : base(2) { }
    }

    /* Capture the flag */

    [Serializable, ModeElement]
    public class FlagComponent : ComponentBase
    {
        public ByteProperty capturingPlayerId;
        public ElapsedUsProperty captureElapsedTimeUs;
    }

    [Serializable, ModeElement]
    public class FlagArrayElement : ArrayElement<FlagComponent>
    {
        public FlagArrayElement() : base(2) { }
    }

    [Serializable, ModeElement]
    public class CtfComponent : ComponentBase
    {
        public ArrayElement<FlagArrayElement> teamFlags = new ArrayElement<FlagArrayElement>(2);
    }

    /* Secure area */

    [Serializable, ModeElement]
    public class SiteComponent : ComponentBase
    {
        public TimeUsProperty timeUs;
        public BoolProperty isRedInside, isBlueInside;
    }

    [Serializable, ModeElement]
    public class SiteElement : ArrayElement<SiteComponent>
    {
        public SiteElement() : base(2) { }
    }

    [Serializable, ModeElement]
    public class SecureAreaComponent : ComponentBase
    {
        public SiteElement sites = new SiteElement();
        public TimeUsProperty roundTime;
        [NonSerialized] public ByteProperty lastWinningTeam;

        public bool RedInside(out SiteComponent element)
        {
            foreach (SiteComponent site in sites)
            {
                if (site.isRedInside)
                {
                    element = site;
                    return true;
                }
            }
            element = default;
            return false;
        }
    }

    [Serializable, ModeElement]
    public class CurePackageComponent : ComponentBase
    {
        public BoolProperty isActive;
    }

    /* Player */

    [Serializable, OnlyServerTrusted, ModeElement]
    public class ShowdownPlayerComponent : ComponentBase
    {
        public ByteProperty stagesCuredFlags;
        public ElapsedUsProperty elapsedSecuringUs;

        public bool IsCured(ShowdownSessionComponent showdown) => (stagesCuredFlags & (byte) (1 << showdown.number)) != 0;
    }

    [Serializable, ClientTrusted, ModeElement]
    public class DesignerPlayerComponent : ComponentBase
    {
        public Position3IntProperty positionOne, positionTwo;
        public VoxelChangeProperty selectedVoxel;
        public UShortProperty selectedModelId;
        public FloatProperty editRadius;
    }

    [Serializable, ModeElement]
    public class MoneyComponent : ComponentBase
    {
        [OnlyServerTrusted] public UShortProperty count;
    }

    [Serializable, SingleTick]
    public class WantedItemComponent : ComponentBase
    {
        public ByteProperty id;
    }

    [Serializable]
    public class WantedItemIdsComponent : ArrayElement<ByteProperty>
    {
        public WantedItemIdsComponent() : base(InventoryComponent.ItemsCount) { }
    }

    [Serializable, OnlyServerTrusted, ModeElement]
    public class BrokeVoxelTickProperty : ByteProperty
    {
    }

    [Serializable]
    public class ExtentsProperty : VectorProperty
    {
        public ExtentsProperty() { }
        public ExtentsProperty(float x, float y, float z) : base(x, y, z) { }
    }

    public static class VoxelfieldComponents
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
            => SerializationRegistrar.RegisterAll(typeof(ModelIdProperty), typeof(ByteIdProperty), typeof(TeamProperty),
                                                  typeof(VectorProperty), typeof(ModeIdProperty), typeof(ExtentsProperty));

        public static readonly SessionElements SessionElements;

        static VoxelfieldComponents()
        {
            SessionElements = SessionElements.NewStandardSessionElements();
            SessionElements.playerElements.AppendAll(typeof(ShowdownPlayerComponent), typeof(DesignerPlayerComponent), typeof(SecureAreaComponent), typeof(MoneyComponent),
                                                     typeof(BrokeVoxelTickProperty));
            SessionElements.commandElements.AppendAll(typeof(WantedItemComponent), typeof(WantedItemIdsComponent));
            SessionElements.elements.AppendAll(typeof(VoxelMapNameProperty), typeof(OrderedVoxelChangesProperty),
                                               typeof(CtfComponent), typeof(ShowdownSessionComponent), typeof(SecureAreaComponent), typeof(DualScoresComponent));
        }
    }
}