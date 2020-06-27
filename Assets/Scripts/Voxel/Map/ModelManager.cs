using System.Collections.Generic;
using Swihoni.Util;
using UnityEngine;

namespace Voxel.Map
{
    public class ModelManager : SingletonBehavior<ModelManager>
    {
        [SerializeField] private GameObject[] m_Models = default;

        private readonly List<GameObject> m_ActiveModels = new List<GameObject>(64);

        public GameObject[] Models => m_Models;

        public void LoadInModel(int modelId, in Vector3 position, in Quaternion rotation)
        {
            GameObject model = Instantiate(m_Models[modelId], position, rotation);
            m_ActiveModels.Add(model);
        }

        public void ClearAllModels()
        {
            foreach (GameObject activeModel in m_ActiveModels)
                Destroy(activeModel);
            m_ActiveModels.Clear();
        }
    }
}