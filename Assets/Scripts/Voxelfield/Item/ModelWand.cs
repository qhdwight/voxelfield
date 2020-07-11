using Console;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel.Map;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield.Item
{
    public class ModelWand : SculptingItem
    {
        private static readonly string[] Commands = {"select_model"};

        [SerializeField] private LayerMask m_ModelMask = default;

        protected override void OnEquip(SessionBase session, int playerId, ItemComponent item, uint durationUs)
            => VoxelWand.SessionCommand(session, playerId, Commands);

        protected override void OnUnequip(SessionBase session, int playerId, ItemComponent item, uint durationUs)
            => ConsoleCommandExecutor.RemoveCommands(Commands);

        protected override void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            if (session.GetModifyingPayerFromId(playerId).Without<ServerTag>()) return;

            Ray ray = session.GetRayForPlayerId(playerId);
            if (!Physics.Raycast(ray, out RaycastHit hit, m_EditDistance, m_ModelMask)) return;

            var modelBehavior = hit.collider.GetComponentInParent<ModelBehaviorBase>();
            if (modelBehavior)
            {
                MapManager.Singleton.RemoveModel(modelBehavior.Position);
            }
        }

        protected override void SecondaryUse(SessionBase session, int playerId, uint durationUs)
        {
            if (WithoutServerHit(session, playerId, m_EditDistance, out RaycastHit hit)) return;

            var position = (Position3Int) (hit.point + hit.normal * 0.5f);

            var designer = session.GetModifyingPayerFromId(playerId).Require<DesignerPlayerComponent>();
            if (designer.selectedModelId.WithoutValue) return;
            ushort selectedModelId = designer.selectedModelId;

            var model = new Container(new ModelIdProperty(selectedModelId));
            switch (selectedModelId)
            {
                case ModelsProperty.Flag:
                case ModelsProperty.Spawn:
                {
                    model.Append(new ModeIdProperty(ModeIdProperty.Ctf));
                    model.Append(new TeamProperty(CtfMode.RedTeam));
                    break;
                }
            }
            MapManager.Singleton.AddModel(position, model);
        }

        public override void ModifyChecked(SessionBase session, int playerId, Container player, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs,
                                           uint durationUs)
        {
            base.ModifyChecked(session, playerId, player, item, inventory, inputs, durationUs);

            if (player.WithoutPropertyOrWithoutValue(out StringCommandProperty command) || command.Builder.Length == 0) return;

            string[] split = command.Builder.ToString().Split();
            switch (split[0])
            {
                case "select_model":
                    if (split.Length > 1 && ushort.TryParse(split[1], out ushort modelId))
                        player.Require<DesignerPlayerComponent>().selectedModelId.Value = modelId;
                    break;
            }
        }
    }
}