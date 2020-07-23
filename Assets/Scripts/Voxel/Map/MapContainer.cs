using System;
using LiteNetLib.Utils;
using Swihoni.Components;
using UnityEngine;

namespace Voxel.Map
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

        public ChangedVoxelsProperty changedVoxels;
        public ModelsProperty models;
        public BoolProperty breakableEdges;

        public MapContainer Deserialize(NetDataReader reader)
        {
            foreach (ElementBase element in Elements)
            {
                element.Deserialize(reader);
                if (ReferenceEquals(element, version)) changedVoxels.Version = version.AsNewString();
            }
            if (version != Application.version) Debug.Log($"Converting map container to newest version, from {version} to {Application.version}");
            return this;
        }
    }
}