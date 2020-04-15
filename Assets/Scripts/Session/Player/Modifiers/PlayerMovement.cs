using Components;
using Input;
using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : PlayerModifierBehaviorBase
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

        public override void ModifyChecked(Container containerToModify, Container commands, float duration)
        {
            base.ModifyChecked(containerToModify, commands, duration);
            FullMove(containerToModify, commands, duration);
        }

        public override void ModifyCommands(Container commandsToModify)
        {
            if (!commandsToModify.If(out InputFlagProperty inputProperty)) return;

            InputProvider input = InputProvider.Singleton;
            inputProperty.SetInput(PlayerInput.Forward, input.GetInput(InputType.Forward));
            inputProperty.SetInput(PlayerInput.Backward, input.GetInput(InputType.Backward));
            inputProperty.SetInput(PlayerInput.Right, input.GetInput(InputType.Right));
            inputProperty.SetInput(PlayerInput.Left, input.GetInput(InputType.Left));
            inputProperty.SetInput(PlayerInput.Jump, input.GetInput(InputType.Jump));
        }

        protected override void SynchronizeBehavior(Container containersToApply)
        {
            if (!containersToApply.If(out MoveComponent moveComponent)
             || !containersToApply.If(out HealthProperty healthProperty)) return;

            if (moveComponent.position.HasValue) transform.position = moveComponent.position;
            if (healthProperty.HasValue) m_Controller.enabled = healthProperty.IsAlive;
        }

        private void FullMove(Container containerToModify, Container commands, float duration)
        {
            if (!containerToModify.If(out MoveComponent moveComponent)
             || !containerToModify.If(out HealthProperty healthProperty)
             || healthProperty.IsDead
             || !commands.If(out InputFlagProperty inputProperty)) return;

            Vector3 initialVelocity = moveComponent.velocity, endingVelocity = initialVelocity;
            float lateralSpeed = LateralMagnitude(endingVelocity);
            Transform playerTransform = transform;
            bool isGrounded = Physics.RaycastNonAlloc(playerTransform.position + new Vector3 {y = RaycastOffset}, Vector3.down, m_CachedGroundHits,
                                                      m_MaxStickDistance + RaycastOffset, m_GroundMask) > 0,
                 withinAngleLimit = isGrounded && Vector3.Angle(m_CachedGroundHits[0].normal, Vector3.up) < m_Controller.slopeLimit;
            if (withinAngleLimit && endingVelocity.y < 0.0f) // Stick to ground. Only on way down, if done on way up it negates jump
            {
                float distance = m_CachedGroundHits[0].distance;
                endingVelocity.y = DefaultDownSpeed;
                if (!inputProperty.GetInput(PlayerInput.Jump)) m_Controller.Move(new Vector3 {y = -distance - 0.06f});
            }
            Vector3 wishDirection =
                inputProperty.GetAxis(PlayerInput.Forward, PlayerInput.Backward) * m_ForwardSpeed * playerTransform.forward +
                inputProperty.GetAxis(PlayerInput.Right, PlayerInput.Left) * m_SideSpeed * playerTransform.right;
            float wishSpeed = wishDirection.magnitude;
            wishDirection.Normalize();
            if (wishSpeed > m_MaxSpeed) wishSpeed = m_MaxSpeed;
            if (isGrounded && withinAngleLimit)
            {
                if (moveComponent.groundTick >= 1)
                {
                    if (lateralSpeed > m_FrictionCutoff) Friction(lateralSpeed, duration, ref endingVelocity);
                    else if (Mathf.Approximately(wishSpeed, 0.0f)) endingVelocity = Vector3.zero;
                    endingVelocity.y = DefaultDownSpeed;
                }
                Accelerate(wishDirection, wishSpeed, m_Acceleration, duration, ref endingVelocity);
                if (inputProperty.GetInput(PlayerInput.Jump))
                {
                    initialVelocity.y = m_JumpSpeed;
                    endingVelocity.y = initialVelocity.y - m_GravityFactor * duration;
                }
                if (moveComponent.groundTick < byte.MaxValue) moveComponent.groundTick.Value++;
            }
            else
            {
                moveComponent.groundTick.Value = 0;
                if (wishSpeed > m_AirSpeedCap) wishSpeed = m_AirSpeedCap;
                Accelerate(wishDirection, wishSpeed, m_AirAcceleration, duration, ref endingVelocity);
                endingVelocity.y -= m_GravityFactor * duration;
                float lateralAirSpeed = LateralMagnitude(endingVelocity);
                if (lateralAirSpeed > m_MaxAirSpeed)
                {
                    endingVelocity.x *= m_MaxAirSpeed / lateralAirSpeed;
                    endingVelocity.z *= m_MaxAirSpeed / lateralAirSpeed;
                }
            }
            m_Controller.Move((initialVelocity + endingVelocity) / 2.0f * duration);
            moveComponent.position.Value = transform.position;
            moveComponent.velocity.Value = endingVelocity;
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