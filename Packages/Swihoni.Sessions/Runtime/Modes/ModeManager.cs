using System.Linq;
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
    }
}