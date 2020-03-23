using UnityEngine;

namespace Session.Player
{
    public class PlayerVisualsBehavior : MonoBehaviour
    {
        public void Visualize(PlayerData data)
        {
            transform.position = data.position;
        }

        public void SetVisible(bool isVisible)
        {
        }
    }
}