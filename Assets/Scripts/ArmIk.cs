using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class ArmIk : MonoBehaviour
{
    private const int BoneCount = 3, HandIndex = 0, ForearmIndex = 1, UpperArmIndex = 2;

    [SerializeField] private Transform m_LeftHand, m_RightHand, m_LeftTarget, m_RightTarget;

    private readonly Transform[] m_LeftArm = new Transform[BoneCount], m_RightArm = new Transform[BoneCount];
    private float m_RightUpperArmLength, m_RightForearmLength;
    private bool m_IsSetup;

    private static bool SetupArm(Transform hand, IList<Transform> arm)
    {
        if (hand == null) return false;
        Transform current = hand;
        for (var boneIndex = 0; boneIndex < BoneCount; boneIndex++)
        {
            if (current == null) return false;
            arm[boneIndex] = current;
            current = current.parent;
        }
        return true;
    }

    private void LateUpdate()
    {
        if (SetupArm(m_RightHand, m_RightArm) && m_RightTarget != null)
        {
            m_RightForearmLength = Vector3.Distance(m_RightArm[HandIndex].position, m_RightArm[ForearmIndex].position);
            m_RightUpperArmLength = Vector3.Distance(m_RightArm[ForearmIndex].position, m_RightArm[UpperArmIndex].position);
            float lengthToTarget = Vector3.Distance(m_RightTarget.position, m_RightArm[UpperArmIndex].position);

            Transform upperArm = m_RightArm[UpperArmIndex];
            Vector3 direction = m_RightTarget.localPosition - upperArm.localPosition;
            float z = Mathf.Rad2Deg * Mathf.Atan2(-direction.x, direction.y);
            float y = GetCAngle(m_RightUpperArmLength, lengthToTarget, m_RightForearmLength);
            upperArm.localRotation = Quaternion.Euler(0.0f, -y, z);

            Transform forearm = m_RightArm[ForearmIndex];
            float x = GetCAngle(m_RightUpperArmLength, m_RightForearmLength, lengthToTarget);
            forearm.localRotation = Quaternion.Euler(x, 0.0f, 0.0f);
        }
    }

    private static float GetCAngle(float a, float b, float c)
    {
        return Mathf.Rad2Deg * Mathf.Acos((Mathf.Pow(a, 2) + Mathf.Pow(b, 2) - Mathf.Pow(c, 2)) / (2 * a * b));
    }
}