using System;
using UnityEngine;
using UnityEngine.UI;

namespace Voxelfield.Interface.Showdown
{
    public class BuyMenuButton : MonoBehaviour
    {
        [SerializeField] private byte m_ItemId;
        private Button m_Button;

        public byte ItemId => m_ItemId;
        public Button Button => m_Button;

        private void Awake() => m_Button = GetComponent<Button>();
    }
}