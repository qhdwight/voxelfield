using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerMovement : PlayerModifierBehaviorBase
    {
        private const float DefaultDownSpeed = -0.01f, RaycastOffset = 0.1f;

        public const byte Upright = 0, Crouched = 1;

        private readonly RaycastHit[] m_CachedGroundHits = new RaycastHit[1];
        private readonly Collider[] m_CachedContactColliders = new Collider[2];

        [SerializeField] private Transform m_MoveTransform = default;

        [SerializeField] private float
            m_Acceleration = 10.0f,
            m_AirAcceleration = 20.0f,
            m_AirSpeedCap = 2.0f,
            m_RunSpeed = 6.0f,
            m_CrouchSpeed = 6.0f,
            m_SprintMultiplier = 1.3f,
            m_WalkMultiplier = 0.3f,
            m_MaxAirSpeed = 8.0f,
            m_Friction = 10.0f,
            m_FrictionCutoff = 0.1f,
            m_StopSpeed = 1.0f,
            m_JumpSpeed = 8.5f,
            m_ForwardSpeed = 30.0f,
            m_SideSpeed = 30.0f,
            m_GravityFactor = 23.0f,
            m_MaxStickDistance = 0.25f,
            m_FlySpeed = 5.0f,
            m_MaxSlopeNerf = 0.3f;
        [SerializeField] private float m_WalkStateDuration = 1.0f, m_CrouchDuration = 0.3f;
        [SerializeField] private LayerMask m_GroundMask = default;

        private CharacterController m_Controller;
        private CharacterControllerListener m_ControllerListener;
        private float m_ControllerHeight;
        private Vector3 m_ControllerCenter;

        public ControllerColliderHit Hit => m_ControllerListener.CachedControllerHit;

        public LayerMask GroundMask => m_GroundMask;
        public float MaxSpeed => m_RunSpeed * m_SprintMultiplier;
        public Collider Collider => m_Controller;

        internal override void Setup()
        {
            m_Controller = m_MoveTransform.GetComponent<CharacterController>();
            m_ControllerListener = m_MoveTransform.GetComponent<CharacterControllerListener>();
            m_Controller.gameObject.SetActive(false);
            m_ControllerHeight = m_Controller.height;
            m_ControllerCenter = m_Controller.center;
        }

        protected internal override void SynchronizeBehavior(in SessionContext context)
        {
            Container player = context.player;
            var move = player.Require<MoveComponent>();
            bool isControllerActive;
            if (move.position.WithValue)
            {
                m_MoveTransform.position = move.position;
                if (player.With(out CameraComponent playerCamera))
                    m_MoveTransform.transform.rotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up);
                isControllerActive = player.Without(out HealthProperty health) || health.IsActiveAndAlive;
                float weight = Mathf.Lerp(0.7f, 1.0f, 1.0f - move.normalizedCrouch);
                m_Controller.height = m_ControllerHeight * weight;
                m_Controller.center = m_ControllerCenter * weight;
            }
            else isControllerActive = false;
            m_Controller.gameObject.SetActive(isControllerActive);
        }

        public override void ModifyChecked(in SessionContext context)
        {
            Container player = context.player;
            if (player.Without(out MoveComponent move)) return;

            base.ModifyChecked(in context); // Synchronize game object

            if (player.WithPropertyWithValue(out FrozenProperty frozen) && frozen
             || player.With(out HealthProperty health) && health.IsDead
             || context.commands.Without(out InputFlagProperty inputs)) return;

            if (context.sessionContainer.Require<AllowCheatsProperty>())
            {
                bool flyInput = inputs.GetInput(PlayerInput.Fly);
                if (flyInput) move.type.Value = move.type == MoveType.Grounded ? MoveType.Flying : MoveType.Grounded;
            }
            else move.type.Value = MoveType.Grounded;

            if (context.tickDelta < 1) return;

            float duration = context.durationUs * TimeConversions.MicrosecondToSecond;

            if (move.type == MoveType.Grounded) FullMove(player, move, inputs, duration);
            else FlyMove(move, inputs, duration);

            ModifyStatus(move, inputs, duration);
        }

        private void FlyMove(MoveComponent move, InputFlagProperty inputs, float duration)
        {
            float forwards = inputs.GetAxis(PlayerInput.Forward, PlayerInput.Backward),
                  right = inputs.GetAxis(PlayerInput.Right, PlayerInput.Left),
                  up = inputs.GetAxis(PlayerInput.Jump, PlayerInput.Crouch),
                  speedMultiplier = 1.0f;

            if (inputs.GetInput(PlayerInput.Walk)) speedMultiplier *= m_WalkMultiplier;
            if (inputs.GetInput(PlayerInput.Sprint)) speedMultiplier *= m_SprintMultiplier * 4.0f;

            m_MoveTransform.Translate(duration * speedMultiplier * m_FlySpeed * new Vector3 {x = right, y = up, z = forwards});
            move.position.Value = m_MoveTransform.position;
            move.velocity.Value = Vector3.zero;
            move.groundTick.Value = byte.MaxValue;
        }

        public override void ModifyTrusted(in SessionContext context, Container verifiedPlayer) { }

        public override void ModifyCommands(SessionBase session, Container commands, int playerId)
        {
            if (commands.Without(out InputFlagProperty inputs)) return;

            inputs.SetInput(PlayerInput.Forward, InputProvider.GetInput(PlayerInput.Forward));
            inputs.SetInput(PlayerInput.Backward, InputProvider.GetInput(PlayerInput.Backward));
            inputs.SetInput(PlayerInput.Right, InputProvider.GetInput(PlayerInput.Right));
            inputs.SetInput(PlayerInput.Left, InputProvider.GetInput(PlayerInput.Left));
            inputs.SetInput(PlayerInput.Jump, InputProvider.GetInput(PlayerInput.Jump));
            inputs.SetInput(PlayerInput.Crouch, InputProvider.GetInput(PlayerInput.Crouch));
            inputs.SetInput(PlayerInput.Sprint, InputProvider.GetInput(PlayerInput.Sprint));
            inputs.SetInput(PlayerInput.Walk, InputProvider.GetInput(PlayerInput.Walk));
            inputs.SetInput(PlayerInput.Suicide, InputProvider.GetInput(PlayerInput.Suicide));
            inputs.SetInput(PlayerInput.Interact, InputProvider.GetInput(PlayerInput.Interact));
            inputs.SetInput(PlayerInput.Respawn, InputProvider.GetInput(PlayerInput.Respawn));
            if (InputProvider.GetInputDown(PlayerInput.Fly)) inputs.SetInput(PlayerInput.Fly);
        }

        private void ModifyStatus(MoveComponent move, InputFlagProperty inputs, float duration)
        {
            if (inputs.GetInput(PlayerInput.Crouch))
            {
                move.normalizedCrouch.Value += duration / m_CrouchDuration;
                if (move.normalizedCrouch > 1.0f) move.normalizedCrouch.Value = 1.0f;
            }
            else
            {
                move.normalizedCrouch.Value -= duration / m_CrouchDuration;
                if (move.normalizedCrouch < 0.0f) move.normalizedCrouch.Value = 0.0f;
            }

            if (ExtraMath.LateralMagnitude(move.velocity) < 1e-2f) move.normalizedMove.Value = 0.0f;
            else
            {
                float stateDuration = m_WalkStateDuration;
                if (inputs.GetInput(PlayerInput.Crouch)) stateDuration /= m_CrouchSpeed / m_RunSpeed;
                if (inputs.GetInput(PlayerInput.Walk)) stateDuration /= m_WalkMultiplier;
                if (inputs.GetInput(PlayerInput.Sprint)) stateDuration /= m_SprintMultiplier;
                move.normalizedMove.Value += duration / stateDuration;
                while (move.normalizedMove > 1.0f)
                    move.normalizedMove.Value -= 1.0f;
            }
        }

        private void FullMove(Container player, MoveComponent move, InputFlagProperty inputs, float duration)
        {
            Vector3 initialVelocity = move.velocity, endingVelocity = initialVelocity;
            float lateralSpeed = endingVelocity.LateralMagnitude();

            Vector3 position = m_MoveTransform.position;
            int capsuleCastCount = OverlapGround(position);
            int downwardCastCount = Physics.RaycastNonAlloc(position + new Vector3 {y = RaycastOffset}, Vector3.down, m_CachedGroundHits,
                                                            float.PositiveInfinity, m_GroundMask);
            m_CachedGroundHits.SortByDistance(downwardCastCount);
            float floorDistance = m_CachedGroundHits.First().distance,
                  slopeAngle = Vector3.Angle(Hit.normal, Vector3.up);
            bool isGrounded = m_Controller.isGrounded || capsuleCastCount == 2, // Always have 1 due to ourselves. 2 means we are touching something else
                 withinAngleLimit = isGrounded && slopeAngle < m_Controller.slopeLimit,
                 applyStick = endingVelocity.y < 0.0f && withinAngleLimit; // Only on way down, if done on way up it negates jump

            Vector3 wishDirection =
                inputs.GetAxis(PlayerInput.Forward, PlayerInput.Backward) * m_ForwardSpeed * m_MoveTransform.forward +
                inputs.GetAxis(PlayerInput.Right, PlayerInput.Left) * m_SideSpeed * m_MoveTransform.right;
            float wishSpeed = wishDirection.magnitude;
            wishDirection.Normalize();

            float maxSpeed = CalculateMaxSpeed(player, inputs, slopeAngle);

            if (wishSpeed > maxSpeed) wishSpeed = maxSpeed;
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
                float lateralAirSpeed = endingVelocity.LateralMagnitude();
                if (lateralAirSpeed > m_MaxAirSpeed)
                {
                    endingVelocity.x *= m_MaxAirSpeed / lateralAirSpeed;
                    endingVelocity.z *= m_MaxAirSpeed / lateralAirSpeed;
                }
            }

            // Prevent sticking to ceiling
            if (!isGrounded && endingVelocity.y > 0.0f && Physics.RaycastNonAlloc(position + new Vector3 {y = m_Controller.height},
                                                                                  Vector3.up, m_CachedGroundHits, RaycastOffset, m_GroundMask) > 0)
                endingVelocity.y = 0.0f;

            Vector3 motion = (initialVelocity + endingVelocity) / 2.0f * duration;

            // Prevent player from walking off the side when crouching
            if (inputs.GetInput(PlayerInput.Crouch) && !inputs.GetInput(PlayerInput.Jump) && isGrounded)
            {
                int projectedCount = OverlapGround(position + motion);
                if (projectedCount == 1)
                {
                    move.position.Value = position;
                    move.velocity.Value = Vector3.zero;
                    return;
                }
            }

            if (applyStick && downwardCastCount > 0)
            {
                // endingVelocity.y = DefaultDownSpeed;
                if (!inputs.GetInput(PlayerInput.Jump) && floorDistance < m_MaxStickDistance + RaycastOffset)
                    // m_Controller.Move(new Vector3 {y = -distance - RaycastOffset});
                    motion.y = -floorDistance - RaycastOffset;
            }

            m_Controller.Move(motion);
            move.position.Value = m_MoveTransform.position;
            move.velocity.Value = endingVelocity;
        }

        private int OverlapGround(in Vector3 position)
        {
            float radius = m_Controller.radius - 0.01f;
            return Physics.OverlapCapsuleNonAlloc(position + new Vector3 {y = radius - m_MaxStickDistance},
                                                  position + new Vector3 {y = m_Controller.height / 2.0f},
                                                  radius, m_CachedContactColliders, m_GroundMask);
        }

        private float CalculateMaxSpeed(Container player, InputFlagProperty inputs, float slopeAngle)
        {
            float maxSpeed = inputs.GetInput(PlayerInput.Crouch) ? m_CrouchSpeed : m_RunSpeed;
            if (inputs.GetInput(PlayerInput.Walk)) maxSpeed *= m_WalkMultiplier;
            if (inputs.GetInput(PlayerInput.Sprint)) maxSpeed *= m_SprintMultiplier;
            if (player.With(out InventoryComponent inventory) && inventory.WithItemEquipped(out ItemComponent equippedItem))
                maxSpeed *= ItemAssetLink.GetModifier(equippedItem.id).movementFactor;
            float slopeMultiplier = m_MaxSlopeNerf + (1.0f - m_MaxSlopeNerf) * (1.0f - Mathf.Clamp01(slopeAngle / m_Controller.slopeLimit));
            maxSpeed *= slopeMultiplier;
            return maxSpeed;
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