using System.Text;
using Swihoni.Sessions.Interfaces;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Voxelfield.Interface.Showdown
{
    public abstract class RoundInterfaceBase : SessionInterfaceBehavior
    {
        [SerializeField] protected BufferedTextGui m_UpperText = default;
        [SerializeField] protected ProgressInterface m_SecuringProgress = default;
    }

    internal static class RoundInterfaceExtensions
    {
        internal static StringBuilder AppendTime(this StringBuilder builder, uint totalUs)
        {
            uint minutes = totalUs / 60_000_000u, seconds = totalUs / 1_000_000u % 60u;
            builder.Append(minutes).Append(":");
            if (seconds < 10) builder.Append("0");
            builder.Append(seconds);
            return builder;
        }
    }
}