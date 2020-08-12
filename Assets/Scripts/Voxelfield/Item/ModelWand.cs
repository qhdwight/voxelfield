using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util.Math;
using UnityEngine;
using Voxelfield.Session;
using Voxelfield.Session.Mode;
using Voxels.Map;

namespace Voxelfield.Item
{
    public class ModelWand : SculptingItem
    {
        private static readonly string[] Commands = {"select_model"};

        [SerializeField] private LayerMask m_ModelMask = default;

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeCommands() => SessionBase.RegisterSessionCommand("select_model");

        protected override void Swing(in SessionContext context, ItemComponent item)
        {
            if (context.player.Without<ServerTag>()) return;

            Ray ray = context.session.GetRayForPlayerId(context.playerId);
            if (!Physics.Raycast(ray, out RaycastHit hit, m_EditDistance, m_ModelMask)) return;

            var modelBehavior = hit.collider.GetComponentInParent<ModelBehaviorBase>();
            if (modelBehavior)
            {
                MapManager.Singleton.RemoveModel(modelBehavior.Position);
            }
        }

        protected override void SecondaryUse(in SessionContext context)
        {
            if (WithoutHit(context, m_EditDistance, out RaycastHit hit)) return;

            var position = (Position3Int) (hit.point + hit.normal * 0.5f);

            var designer = context.player.Require<DesignerPlayerComponent>();
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
                case ModelsProperty.Site:
                {
                    model.Append(new ModeIdProperty(ModeIdProperty.SecureArea));
                    model.Append(new ExtentsProperty(1, 1, 1));
                    break;
                }
            }
            MapManager.Singleton.AddModel(position, model);
        }

        public override void ModifyChecked(in SessionContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            base.ModifyChecked(context, item, inventory, inputs);

            if (!context.WithServerStringCommands(out IEnumerable<string[]> commands)) return;
            foreach (string[] arguments in commands)
            {
                switch (arguments[0])
                {
                    case "select_model":
                        if (arguments.Length > 1 && ushort.TryParse(arguments[1], out ushort modelId))
                            context.player.Require<DesignerPlayerComponent>().selectedModelId.Value = modelId;
                        break;
                }
            }
        }
    }
}