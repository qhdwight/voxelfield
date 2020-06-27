using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Compound.Session.Mode
{
    [CreateAssetMenu(fileName = "Designer", menuName = "Session/Mode/Designer", order = 0)]
    public class DesignerMode : ModeBase
    {
        public override void SpawnPlayer(SessionBase session, Container player)
        {
            // TODO:refactor zeroing
            if (player.With(out MoveComponent move))
            {
                move.Zero();
                move.position.Value = new Vector3 {y = 10.0f};
            }
            player.Require<IdProperty>().Value = 1;
            player.Require<UsernameElement>().SetString("Designer");
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health))
                health.Value = 100;
            player.ZeroIfWith<RespawnTimerProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            player.ZeroIfWith<DamageNotifierComponent>();
            player.ZeroIfWith<InventoryComponent>();
        }
    }
}