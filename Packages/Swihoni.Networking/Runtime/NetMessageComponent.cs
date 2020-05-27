using System;
using System.IO;
using Swihoni.Components;

namespace Swihoni.Networking
{
    [Serializable]
    public class NetMessageComponent
    {
        private const int InitialBufferSize = 1 << 16;
        
        private readonly ElementBase m_Element;

        public ElementBase Element => m_Element;

        public NetMessageComponent(ElementBase element) => m_Element = element;

        public MemoryStream Stream { get; } = new MemoryStream(InitialBufferSize);
    }
}