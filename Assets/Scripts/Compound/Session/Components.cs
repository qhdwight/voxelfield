using System;
using Session.Components;
using Session.Player.Components;

namespace Compound.Session
{
    [Serializable]
    public class SessionContainer : SessionContainerBase<PlayerContainer>
    {
    }

    [Serializable]
    public class PlayerContainer : StandardPlayerContainer
    {
    }

    [Serializable]
    public class PlayerCommandsContainer : StandardPlayerCommandsContainer
    {
    }
}