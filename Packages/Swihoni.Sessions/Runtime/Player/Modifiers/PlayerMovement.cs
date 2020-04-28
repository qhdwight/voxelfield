using Input;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerMovement : PlayerModifierBehaviorBase
    {
        private const float DefaultDownSpeed = -1.0f, RaycastOffset = 0.05f;

        public const byte Upright = 0, Crouched = 1;

        private readonly RaycastHit[] m_CachedGroundHits = new RaycastHit[1];

        [SerializeField] private Transform m_MoveTransform = default;

        [SerializeField] private float
            m_Acceleration = 10.0f,
            m_AirAcceleration = 20.0f,
            m_AirSpeedCap = 2.0f,
            m_MaxSpeed = 6.0f,
            m_MaxAirSpeed = 8.0f,
            m_Friction = 10.0f,
            m_FrictionCutoff = 0.1f,
            m_StopSpeed = 1.0f,
            m_JumpSpeed = 8.5f,
            m_ForwardSpeed = 30.0f,
            m_SideSpeed = 30.0f,
            m_GravityFactor = 23.0f,
            m_MaxStickDistance = 0.25f;
        [SerializeField] private float m_WalkStateDuration = 1.0f;
        [SerializeField] private LayerMask m_GroundMask = default;

        private CharacterController m_Controller, m_PrefabController;
        
        public LayerMask GroundMask => m_GroundMask;
        public float MaxSpeed => m_MaxSpeed;
        public float WalkStateDuration => m_WalkStateDuration;

        internal override void Setup(SessionBase session)
        {
            base.Setup(session);
            m_Controller = m_MoveTransform.GetComponent<CharacterController>();
            m_Controller.enabled = false;
            m_PrefabController = session.PlayerModifierPrefab.GetComponentInChildren<CharacterController>();
        }

        protected override void SynchronizeBehavior(Container player)
        {
            var move = player.Require<MoveComponent>();
            m_MoveTransform.position = move.position;
            if (player.Has(out CameraComponent playerCamera))
                m_MoveTransform.transform.rotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up);
            m_Controller.enabled = player.Without(out HealthProperty health) || health.HasValue && health.IsAlive;
            float uprightHeight = m_PrefabController.height;
            Vector3 uprightCenter = m_PrefabController.center;
            m_Controller.height = move.stateId == Upright ? uprightHeight : uprightHeight * 0.7f;
            m_Controller.center = move.stateId == Upright ? uprightCenter : uprightCenter * 0.7f;
        }

        public override void ModifyChecked(SessionBase session, int playerId, Container player, Container commands, float duration)
        {
            if (player.Without(out MoveComponent move)
             || player.Has(out HealthProperty health) && health.IsDead
             || commands.Without(out InputFlagProperty inputs)) return;

            base.ModifyChecked(session, playerId, player, commands, duration);
            FullMove(move, inputs, duration);
            ModifyStatus(move, inputs, duration);
        }

        public override void ModifyTrusted(SessionBase session, int playerId, Container player, Container commands, float duration) { }

        private void ModifyStatus(MoveComponent move, InputFlagProperty inputs, float duration)
        {
            move.stateId.Value = inputs.GetInput(PlayerInput.Crouch) ? Crouched : Upright;
            if (VectorMath.LateralMagnitude(move.velocity) < 1e-2f)
            {
                move.moveElapsed.Value = 0.0f;
            }
            else
            {
                move.moveElapsed.Value += duration;
                while (move.moveElapsed > m_WalkStateDuration)
                {
                    move.moveElapsed.Value -= m_WalkStateDuration;
                }
            }
        }

        public override void ModifyCommands(SessionBase session, Container commands)
        {
            if (commands.Without(out InputFlagProperty inputProperty)) return;

            InputProvider input = InputProvider.Singleton;
            inputProperty.SetInput(PlayerInput.Forward, input.GetInput(InputType.Forward));
            inputProperty.SetInput(PlayerInput.Backward, input.GetInput(InputType.Backward));
            inputProperty.SetInput(PlayerInput.Right, input.GetInput(InputType.Right));
            inputProperty.SetInput(PlayerInput.Left, input.GetInput(InputType.Left));
            inputProperty.SetInput(PlayerInput.Jump, input.GetInput(InputType.Jump));
            inputProperty.SetInput(PlayerInput.Crouch, input.GetInput(InputType.Crouch));
            inputProperty.SetInput(PlayerInput.Suicide, input.GetInput(InputType.Suicide));
        }

        private void FullMove(MoveComponent move, InputFlagProperty inputs, float duration)
        {
            Vector3 initialVelocity = move.velocity, endingVelocity = initialVelocity;
            float lateralSpeed = VectorMath.LateralMagnitude(endingVelocity);
            bool isGrounded = Physics.RaycastNonAlloc(m_MoveTransform.position + new Vector3 {y = RaycastOffset}, Vector3.down, m_CachedGroundHits,
                                                      m_MaxStickDistance + RaycastOffset, m_GroundMask) > 0,
                 withinAngleLimit = isGrounded && Vector3.Angle(m_CachedGroundHits[0].normal, Vector3.up) < m_Controller.slopeLimit;
            if (withinAngleLimit && endingVelocity.y < 0.0f) // Stick to ground. Only on way down, if done on way up it negates jump
            {
                float distance = m_CachedGroundHits[0].distance;
                endingVelocity.y = DefaultDownSpeed;
                // if (!inputProperty.GetInput(PlayerInput.Jump)) m_Controller.Move(new Vector3 {y = -distance - 0.06f});
            }
            Vector3 wishDirection =
                inputs.GetAxis(PlayerInput.Forward, PlayerInput.Backward) * m_ForwardSpeed * m_MoveTransform.forward +
                inputs.GetAxis(PlayerInput.Right, PlayerInput.Left) * m_SideSpeed * m_MoveTransform.right;
            float wishSpeed = wishDirection.magnitude;
            wishDirection.Normalize();
            if (wishSpeed > m_MaxSpeed) wishSpeed = m_MaxSpeed;
            if (isGrounded && withinAngleLimit)
            {
                if (move.groundTick >= 1)
                {
                    if (lateralSpeed > m_FrictionCutoff) Friction(lateralSpeed, duration, ref endingVelocity);
                    else if (Mathf.Approximately(wishSpeed, 0.0f)) endingVelocity = Vector3.zero;
                    endingVelocity.y = DefaultDownSpeed;
                }
                Accelerate(wishDirection, wishSpeed, m_Acceleration, duration, ref endingVelocity);
                if (inputs.GetInput(PlayerInput.Jump))
                {
                    initialVelocity.y = m_JumpSpeed;
                    endingVelocity.y = initialVelocity.y - m_GravityFactor * duration;
                }
                if (move.groundTick < byte.MaxValue) move.groundTick.Value++;
            }
            else
            {
                move.groundTick.Value = 0;
                if (wishSpeed > m_AirSpeedCap) wishSpeed = m_AirSpeedCap;
                Accelerate(wishDirection, wishSpeed, m_AirAcceleration, duration, ref endingVelocity);
                endingVelocity.y -= m_GravityFactor * duration;
                float lateralAirSpeed = VectorMath.LateralMagnitude(endingVelocity);
                if (lateralAirSpeed > m_MaxAirSpeed)
                {
                    endingVelocity.x *= m_MaxAirSpeed / lateralAirSpeed;
                    endingVelocity.z *= m_MaxAirSpeed / lateralAirSpeed;
                }
            }
            m_Controller.Move((initialVelocity + endingVelocity) / 2.0f * duration);
            move.position.Value = m_MoveTransform.position;
            move.velocity.Value = endingVelocity;
        }

        private void Friction(float lateralSpeed, float time, ref Vector3 velocity)
        {
            float control = lateralSpeed < m_StopSpeed ? m_StopSpeed : lateralSpeed;
            float drop = control * m_Friction * time;
            float newSpeed = lateralSpeed - drop;
            if (newSpeed < 0) newSpeed = 0;
            newSpeed /= lateralSpeed;
            velocity.x *= newSpeed;
            velocity.z *= newSpeed;
        }

        private static void Accelerate(Vector3 wishDirection, float wishSpeed, float acceleration, float time, ref Vector3 velocity)
        {
            float velocityProjection = Vector3.Dot(velocity, wishDirection);
            float addSpeed = wishSpeed - velocityProjection;
            if (addSpeed <= 0.0f) return;
            float accelerationSpeed = acceleration * wishSpeed * time;
            if (accelerationSpeed > addSpeed) accelerationSpeed = addSpeed;
            velocity.x += wishDirection.x * accelerationSpeed;
            velocity.z += wishDirection.z * accelerationSpeed;
        }
    }
}