using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

[ExecuteInEditMode]
public class InverseKinematics : MonoBehaviour
{
    [SerializeField] private int m_ChainLength = 2;

    [SerializeField] private Transform m_Target, m_Pole;

    [Header("Solver Parameters")] [SerializeField]
    private int m_Iterations = 10;

    [SerializeField, Tooltip("Distance when the solver stops")]
    private float m_Delta = 0.001f;

    [Range(0, 1), SerializeField, Tooltip("Strength of going back to the start position")]
    private float m_SnapBackStrength = 1.0f;

    private float[] m_BonesLength; // Target to Origin
    private float m_CompleteLength;
    private Transform[] m_Joints;
    private Vector3[] m_JointPositions, m_StartingDirectionsToChild;
    private Quaternion[] m_StartingBoneRotations;
    private Quaternion m_StartRotationTarget;
    private Transform m_Root;
    private bool m_HasSetup;

    private void Init()
    {
        // Verify we have enough transforms in the scene to match our expected chain length
        m_Root = transform;
        for (var i = 0; i < m_ChainLength; i++)
        {
            if (m_Root == null)
                return;
            m_Root = m_Root.parent;
        }

        m_Joints = new Transform[m_ChainLength + 1];
        m_JointPositions = new Vector3[m_ChainLength + 1];
        m_BonesLength = new float[m_ChainLength];
        m_StartingDirectionsToChild = new Vector3[m_ChainLength + 1];
        m_StartingBoneRotations = new Quaternion[m_ChainLength + 1];

        // Init target
        m_StartRotationTarget = GetRotationRootSpace(m_Target);

        // Init data
        Transform current = transform;
        m_CompleteLength = 0;
        for (int i = m_Joints.Length - 1; i >= 0; i--)
        {
            m_Joints[i] = current;
            m_StartingBoneRotations[i] = GetRotationRootSpace(current);

            if (i == m_Joints.Length - 1)
            {
                // Leaf
                m_StartingDirectionsToChild[i] = GetPositionRootSpace(m_Target) - GetPositionRootSpace(current);
            }
            else
            {
                // Mid bone
                m_StartingDirectionsToChild[i] = GetPositionRootSpace(m_Joints[i + 1]) - GetPositionRootSpace(current);
                m_BonesLength[i] = m_StartingDirectionsToChild[i].magnitude;
                m_CompleteLength += m_BonesLength[i];
            }

            current = current.parent;
        }

        m_HasSetup = true;
    }

    private void LateUpdate()
    {
        ResolveIk();
    }

    private void ResolveIk()
    {
        if (m_Target == null)
            return;

        if (m_BonesLength.Length != m_ChainLength)
            m_HasSetup = false;

        if (!m_HasSetup)
            Init();

        if (!m_HasSetup)
            return;

        // FABRIK solver

        // Get position
        for (var i = 0; i < m_Joints.Length; i++)
            m_JointPositions[i] = GetPositionRootSpace(m_Joints[i]);

        Vector3 targetPosition = GetPositionRootSpace(m_Target);
        Quaternion targetRotation = GetRotationRootSpace(m_Target);

        // 1st is possible to reach?
        if ((targetPosition - GetPositionRootSpace(m_Joints[0])).sqrMagnitude >= m_CompleteLength * m_CompleteLength)
        {
            // Just stretch it
            Vector3 direction = (targetPosition - m_JointPositions[0]).normalized;
            // Set everything after root
            for (var i = 1; i < m_JointPositions.Length; i++)
                m_JointPositions[i] = m_JointPositions[i - 1] + direction * m_BonesLength[i - 1];
        }
        else
        {
            for (var i = 0; i < m_JointPositions.Length - 1; i++)
                m_JointPositions[i + 1] = Vector3.Lerp(m_JointPositions[i + 1], m_JointPositions[i] + m_StartingDirectionsToChild[i], m_SnapBackStrength);

            for (var iteration = 0; iteration < m_Iterations; iteration++)
            {
                // https://www.youtube.com/watch?v=UNoX65PRehA
                // Back
                for (int i = m_JointPositions.Length - 1; i > 0; i--)
                {
                    m_JointPositions[i] = i == m_JointPositions.Length - 1
                        ? targetPosition
                        : m_JointPositions[i + 1] + (m_JointPositions[i] - m_JointPositions[i + 1]).normalized * m_BonesLength[i];
                }

                // Forward
                for (var i = 1; i < m_JointPositions.Length; i++)
                    m_JointPositions[i] = m_JointPositions[i - 1] + (m_JointPositions[i] - m_JointPositions[i - 1]).normalized * m_BonesLength[i - 1];

                // Close enough?
                if ((m_JointPositions[m_JointPositions.Length - 1] - targetPosition).sqrMagnitude < m_Delta * m_Delta)
                    break;
            }
        }

        // Move towards pole
        if (m_Pole != null)
        {
            Vector3 polePosition = GetPositionRootSpace(m_Pole);
            for (var i = 1; i < m_JointPositions.Length - 1; i++)
            {
                var plane = new Plane(m_JointPositions[i + 1] - m_JointPositions[i - 1], m_JointPositions[i - 1]);
                Vector3 projectedPole = plane.ClosestPointOnPlane(polePosition),
                        projectedBone = plane.ClosestPointOnPlane(m_JointPositions[i]);
                float angle = Vector3.SignedAngle(projectedBone - m_JointPositions[i - 1], projectedPole - m_JointPositions[i - 1], plane.normal);
                m_JointPositions[i] = Quaternion.AngleAxis(angle, plane.normal) * (m_JointPositions[i] - m_JointPositions[i - 1]) + m_JointPositions[i - 1];
            }
        }

        // Set position & rotation
        for (var i = 0; i < m_JointPositions.Length; i++)
        {
            SetRootSpace(m_Joints[i], m_JointPositions[i], i == m_JointPositions.Length - 1
                             ? Quaternion.Inverse(targetRotation) * m_StartRotationTarget * Quaternion.Inverse(m_StartingBoneRotations[i])
                             : Quaternion.FromToRotation(m_StartingDirectionsToChild[i], m_JointPositions[i + 1] - m_JointPositions[i]) *
                               Quaternion.Inverse(m_StartingBoneRotations[i]));
        }
    }

    private Vector3 GetPositionRootSpace(Transform current)
    {
        return Quaternion.Inverse(m_Root.rotation) * (current.position - m_Root.position);
    }

    private Quaternion GetRotationRootSpace(Transform current)
    {
        // inverse(after) * before => rot: before -> after
        return Quaternion.Inverse(current.rotation) * m_Root.rotation;
    }

    private void SetRootSpace(Transform current, Vector3 position, Quaternion rotation)
    {
        Quaternion rootRotation = m_Root.rotation;
        current.position = rootRotation * position + m_Root.position;
        current.rotation = rootRotation * rotation;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Transform current = transform;
        for (var i = 0; i < m_ChainLength && current != null && current.parent != null; i++)
        {
            Vector3 parentPosition = current.parent.position, position = current.position;
            float scale = Vector3.Distance(position, parentPosition) * 0.1f;
            Handles.matrix = Matrix4x4.TRS(position, Quaternion.FromToRotation(Vector3.up, parentPosition - position),
                                           new Vector3(scale, Vector3.Distance(parentPosition, position), scale));
            Handles.color = Color.magenta;
            Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
            current = current.parent;
        }
    }
#endif
}