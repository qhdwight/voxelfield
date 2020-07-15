using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    public static class ModeManager
    {
        private static ModeBase[] _modes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _modes = Resources.LoadAll<ModeBase>("Modes")
                              .OrderBy(modifier =>
                               {
                                   modifier.Clear();
                                   return modifier.id;
                               }).ToArray();
        }

        public static ModeBase GetMode(byte modeId) => _modes[modeId];

        public static ModeBase GetMode(Container session) => GetMode(session.Require<ModeIdProperty>());
    }
}