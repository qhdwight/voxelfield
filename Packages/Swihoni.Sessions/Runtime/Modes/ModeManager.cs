using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    public static class ModeId
    {
        public const byte Warmup = 0;
    }

    public static class ModeManager
    {
        private static readonly ModeBase[] Modes;

        static ModeManager()
        {
            Modes = Resources.LoadAll<ModeBase>("Modes")
                             .OrderBy(modifier => modifier.id).ToArray();
        }

        public static ModeBase GetMode(byte modeId) => Modes[modeId];

        public static ModeBase GetMode(Container session) => GetMode(session.Require<ModeIdProperty>());
    }
}