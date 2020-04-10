using System;
using Session.Components;
using Session.Player;
using Session.Player.Components;

namespace Compound.Session
{
    [Serializable]
    public class SessionContainer : SessionContainerBase<PlayerComponent>
    {
    }

    [Serializable]
    public class PlayerComponent : StandardPlayerContainer
    {
    }

    [Serializable]
    public class PlayerCommandsContainer : StandardPlayerCommandsContainer
    {
    }
}