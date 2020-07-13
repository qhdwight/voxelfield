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
        public VoxelMapNameProperty(string @string) : base(@string, 16) { }
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
    public class DualScoresComponent : ArrayElement<ByteProperty>
    {
        public DualScoresComponent() : base(2) { }
    }

    /* Capture the flag */

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
        public ArrayElement<FlagArrayElement> teamFlags = new ArrayElement<FlagArrayElement>(2);
    }

    /* Secure area */

    [Serializable]
    public class SiteComponent : ComponentBase
    {
        public TimeUsProperty timeUs;
        public BoolProperty isRedInside, isBlueInside;
    }

    [Serializable]
    public class SiteElement : ArrayElement<SiteComponent>
    {
        public SiteElement() : base(2) { }
    }

    [Serializable]
    public class SecureAreaComponent : ComponentBase
    {
        public SiteElement sites = new SiteElement();
        public TimeUsProperty roundTime;

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

    [Serializable, ClientTrusted]
    public class DesignerPlayerComponent : ComponentBase
    {
        public Position3IntProperty positionOne, positionTwo;
        public ByteProperty selectedVoxelId;
        public UShortProperty selectedModelId;
        public FloatProperty editRadius;
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

    [Serializable]
    public class ExtentsProperty : VectorProperty
    {
        public ExtentsProperty() { }
        public ExtentsProperty(float x, float y, float z) : base(x, y, z) { }
    }

    public static class VoxelfieldComponents
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize() =>
            SerializationRegistrar.RegisterAll(typeof(ModelIdProperty), typeof(IdProperty), typeof(TeamProperty), typeof(VectorProperty), typeof(ModeIdProperty),
                                               typeof(ExtentsProperty));

        public static readonly SessionElements SessionElements;

        static VoxelfieldComponents()
        {
            SessionElements = SessionElements.NewStandardSessionElements();
            SessionElements.playerElements.AppendAll(typeof(ShowdownPlayerComponent), typeof(DesignerPlayerComponent), typeof(SecureAreaComponent), typeof(MoneyComponent),
                                                     typeof(BrokeVoxelTickProperty));
            // SessionElements.commandElements.AppendAll(typeof(TeamProperty));
            SessionElements.elements.AppendAll(typeof(VoxelMapNameProperty), typeof(ChangedVoxelsProperty),
                                               typeof(CtfComponent), typeof(ShowdownSessionComponent), typeof(SecureAreaComponent), typeof(DualScoresComponent));
        }
    }
}