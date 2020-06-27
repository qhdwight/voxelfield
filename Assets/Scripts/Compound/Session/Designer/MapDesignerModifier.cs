using Input;
using Swihoni.Sessions;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;

namespace Compound.Session.Designer
{
    [RequireComponent(typeof(CharacterController))]
    public class MapDesignerModifier : ModifierBehaviorBase
    {
        private enum MoveMode
        {
            Grounded,
            Flying
        }

        [SerializeField] private float
            m_FlySpeed = 5.0f,
            m_GroundSpeed = 30.0f,
            m_GravityFactor = 2.0f;
        [SerializeField] private LayerMask m_ChunkMask = default;

        private CharacterController m_Controller;
        private MoveMode m_MoveMode = MoveMode.Flying;
        private float m_Pitch, m_Yaw, m_Gravity;

        private void Awake() => m_Controller = GetComponent<CharacterController>();

        private void Update()
        {
            // if (InterfaceManager.IsAnyInterfaceActive()) return;
            Camera();
            Move();
            Editing();
        }

        private void Move()
        {
            float forwards = InputProvider.Singleton.GetAxis(InputType.Forward, InputType.Backward),
                  right = InputProvider.Singleton.GetAxis(InputType.Right, InputType.Left),
                  up = InputProvider.Singleton.GetAxis(InputType.Jump, InputType.Crouch),
                  speedMultiplier = InputProvider.Singleton.GetInput(InputType.Sprint) ? 2.0f : 1.0f;
            if (UnityEngine.Input.GetKeyDown(KeyCode.F))
                m_MoveMode = m_MoveMode == MoveMode.Grounded ? MoveMode.Flying : MoveMode.Grounded;
            if (m_Controller.isGrounded)
                m_Gravity = -0.1f;
            else
                m_Gravity -= m_GravityFactor * Time.deltaTime;
            switch (m_MoveMode)
            {
                case MoveMode.Grounded:
                    m_Controller.Move(transform.TransformDirection(Time.deltaTime * speedMultiplier * m_GroundSpeed *
                                                                   new Vector3 {x = right, y = m_Gravity, z = forwards}));
                    break;
                case MoveMode.Flying:
                    transform.Translate(Time.deltaTime * speedMultiplier * m_FlySpeed * new Vector3 {x = right, y = up, z = forwards});
                    break;
            }
        }

        private void Camera()
        {
            float
                xMouse = InputProvider.GetMouseInput(MouseMovement.X),
                yMouse = InputProvider.GetMouseInput(MouseMovement.Y),
                xRotation = xMouse * InputProvider.Singleton.Sensitivity,
                yRotation = yMouse * InputProvider.Singleton.Sensitivity;
            m_Yaw += xRotation;
            m_Pitch -= yRotation;
            m_Yaw = Mathf.Repeat(m_Yaw, 360.0f);
            m_Pitch = Mathf.Clamp(m_Pitch, -90.0f, 90.0f);
            transform.localEulerAngles = new Vector3 {x = m_Pitch, y = m_Yaw};
        }

        private void Editing()
        {
            bool wantsBreak = InputProvider.Singleton.GetInput(InputType.UseOne),
                 wantsPlace = InputProvider.Singleton.GetInput(InputType.UseTwo);
            if (!wantsBreak && !wantsPlace) return;
            if (!GetVoxel(out RaycastHit hit)) return;
            var position = (Position3Int) (hit.point + hit.normal / 2 * (wantsPlace ? 1.0f : -1.0f));
            Voxel.Voxel? voxel = ChunkManager.Singleton.GetVoxel(position);
            if (wantsPlace)
            {
                ChunkManager.Singleton.SetVoxelData(position, new VoxelChangeData {renderType = VoxelRenderType.Block, texture = VoxelTexture.Stone});
            }
            else if (voxel.HasValue)
            {
                switch (voxel.Value.renderType)
                {
                    case VoxelRenderType.Block:
                        ChunkManager.Singleton.SetVoxelData(position, new VoxelChangeData {renderType = VoxelRenderType.Smooth});
//                        ChunkManager.Singleton.RevertVoxelToMapSave(position);
                        break;
                    case VoxelRenderType.Smooth:
                        ChunkManager.Singleton.RemoveVoxelRadius(position, 2.0f);
                        break;
                }
            }
        }

        private bool GetVoxel(out RaycastHit hit)
        {
            Transform t = transform;
            return Physics.Raycast(t.position, t.forward, out hit, 8.0f, m_ChunkMask);
        }
    }
}