using System;
using System.Collections.Generic;
using System.IO;
using Swihoni.Components;

namespace Swihoni.Networking
{
    [Serializable]
    public class NetMessageComponent : Container
    {
        public NetMessageComponent(params Type[] types) : base(types) { }

        public NetMessageComponent(IEnumerable<Type> types) : base(types) { }

        private const int InitialBufferSize = 1 << 16;

        public MemoryStream Stream { get; } = new MemoryStream(InitialBufferSize);
    }
}