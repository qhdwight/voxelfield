using System;
using LiteNetLib.Utils;
using Swihoni.Components;
using UnityEngine;

namespace Voxelation.Map
{
    [Serializable]
    public class VersionProperty : StringProperty
    {
    }

    [Serializable]
    public class MapContainer : Container
    {
        public StringProperty name;
        public VersionProperty version;
        public IntProperty terrainHeight;
        public DimensionComponent dimension;
        public NoiseComponent noise;

        public VoxelChangesProperty m_VoxelChanges;
        public ModelsProperty models;
        public BoolProperty breakableEdges;

        public MapContainer Deserialize(NetDataReader reader)
        {
            foreach (ElementBase element in Elements)
            {
                element.Deserialize(reader);
                // ReSharper disable once PossibleNullReferenceException
                if (ReferenceEquals(element, version)) m_VoxelChanges.Version = version.AsNewString();
            }
            if (version != Application.version) Debug.Log($"Converting map container to newest version, from {version} to {Application.version}");
            return this;
        }
    }
}