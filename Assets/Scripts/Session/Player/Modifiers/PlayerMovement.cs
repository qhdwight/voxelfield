using Input;
using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : ModifierBehaviorBase<PlayerComponent>
    {
        private const float DefaultDownSpeed = -1.0f, RaycastOffset = 0.05f;
        
        private readonly RaycastHit[] m_CachedGroundHits = new RaycastHit[1];

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

        private CharacterController m_Controller;

        [Header("Movement")] [SerializeField] private LayerMask m_GroundMask = default;

        internal override void Setup()
        {
            m_Controller = GetComponent<CharacterController>();
            m_Controller.enabled = false;
        }

        public override void ModifyChecked(PlayerComponent componentToModify, PlayerCommandsComponent commands)
        {
            base.ModifyChecked(componentToModify, commands);
            FullMove(componentToModify, commands);
        }

        public override void ModifyCommands(PlayerCommandsComponent commandsToModify)
        {
            InputProvider input = InputProvider.Singleton;
            commandsToModify.SetInput(PlayerInput.Forward, input.GetInput(InputType.Forward));
            commandsToModify.SetInput(PlayerInput.Backward, input.GetInput(InputType.Backward));
            commandsToModify.SetInput(PlayerInput.Right, input.GetInput(InputType.Right));
            commandsToModify.SetInput(PlayerInput.Left, input.GetInput(InputType.Left));
            commandsToModify.SetInput(PlayerInput.Jump, input.GetInput(InputType.Jump));
        }

        protected override void SynchronizeBehavior(PlayerComponent componentToApply)
        {
            transform.position = componentToApply.position;
            m_Controller.enabled = componentToApply.IsAlive;
        }

        private void FullMove(PlayerComponent playerComponent, PlayerCommandsComponent commands)
        {
            Vector3 initialVelocity = playerComponent.velocity, endingVelocity = initialVelocity;
            float lateralSpeed = LateralMagnitude(endingVelocity);
            Transform playerTransform = transform;
            bool isGrounded = Physics.RaycastNonAlloc(playerTransform.position + new Vector3 {y = RaycastOffset}, Vector3.down, m_CachedGroundHits,
                                                      m_MaxStickDistance + RaycastOffset, m_GroundMask) > 0,
                 withinAngleLimit = isGrounded && Vector3.Angle(m_CachedGroundHits[0].normal, Vector3.up) < m_Controller.slopeLimit;
            if (withinAngleLimit && endingVelocity.y < 0.0f) // Stick to ground. Only on way down, if done on way up it negates jump
            {
                float distance = m_CachedGroundHits[0].distance;
                endingVelocity.y = DefaultDownSpeed;
                if (!commands.GetInput(PlayerInput.Jump)) m_Controller.Move(new Vector3 {y = -distance - 0.06f});
            }
            Vector3 wishDirection =
                commands.GetAxis(PlayerInput.Forward, PlayerInput.Backward) * m_ForwardSpeed * playerTransform.forward +
                commands.GetAxis(PlayerInput.Right, PlayerInput.Left) * m_SideSpeed * playerTransform.right;
            float wishSpeed = wishDirection.magnitude;
            wishDirection.Normalize();
            if (wishSpeed > m_MaxSpeed) wishSpeed = m_MaxSpeed;
            if (isGrounded && withinAngleLimit)
            {
                if (playerComponent.groundTick >= 1)
                {
                    if (lateralSpeed > m_FrictionCutoff) Friction(lateralSpeed, commands.duration, ref endingVelocity);
                    else if (Mathf.Approximately(wishSpeed, 0.0f)) endingVelocity = Vector3.zero;
                    endingVelocity.y = DefaultDownSpeed;
                }
                Accelerate(wishDirection, wishSpeed, m_Acceleration, commands.duration, ref endingVelocity);
                if (commands.GetInput(PlayerInput.Jump))
                {
                    initialVelocity.y = m_JumpSpeed;
                    endingVelocity.y = initialVelocity.y - m_GravityFactor * commands.duration;
                }
                if (playerComponent.groundTick < byte.MaxValue) playerComponent.groundTick.Value++;
            }
            else
            {
                playerComponent.groundTick.Value = 0;
                if (wishSpeed > m_AirSpeedCap) wishSpeed = m_AirSpeedCap;
                Accelerate(wishDirection, wishSpeed, m_AirAcceleration, commands.duration, ref endingVelocity);
                endingVelocity.y -= m_GravityFactor * commands.duration;
                float lateralAirSpeed = LateralMagnitude(endingVelocity);
                if (lateralAirSpeed > m_MaxAirSpeed)
                {
                    endingVelocity.x *= m_MaxAirSpeed / lateralAirSpeed;
                    endingVelocity.z *= m_MaxAirSpeed / lateralAirSpeed;
                }
            }
            m_Controller.Move((initialVelocity + endingVelocity) / 2.0f * commands.duration);
            playerComponent.position.Value = transform.position;
            playerComponent.velocity.Value = endingVelocity;
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

        private static float LateralMagnitude(Vector3 v)
        {
            return Mathf.Sqrt(v.x * v.x + v.z * v.z);
        }
    }
}