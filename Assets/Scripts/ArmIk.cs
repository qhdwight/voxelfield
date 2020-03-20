using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class ArmIk : MonoBehaviour
{
    private const int BoneCount = 3, HandIndex = 0, ForearmIndex = 1, UpperArmIndex = 2;

    [SerializeField] private Transform m_RightHand, m_RightTarget;

    private readonly Transform[] m_RightArm = new Transform[BoneCount];
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
        if (!SetupArm(m_RightHand, m_RightArm) || m_RightTarget == null) return;
        float rightForearmLength = Vector3.Distance(m_RightArm[HandIndex].position, m_RightArm[ForearmIndex].position),
              rightUpperArmLength = Vector3.Distance(m_RightArm[ForearmIndex].position, m_RightArm[UpperArmIndex].position),
              lengthToTarget = Vector3.Distance(m_RightTarget.position, m_RightArm[UpperArmIndex].position);
        {
            Transform upperArm = m_RightArm[UpperArmIndex];
            Vector3 direction = m_RightTarget.localPosition - upperArm.localPosition;
            float z = Mathf.Rad2Deg * Mathf.Atan2(-direction.x, direction.y);
            float x = GetAngleFromTriangle(rightUpperArmLength, lengthToTarget, rightForearmLength) + Mathf.Rad2Deg * Mathf.Atan2(-direction.z, -direction.y);
            if (!double.IsNaN(x) && !double.IsNaN(z))
                upperArm.localRotation = Quaternion.Euler(0.0f, 0.0f, z) * Quaternion.Euler(360 - x, 0.0f, 0.0f);
        }
        {
            Transform forearm = m_RightArm[ForearmIndex];
            float x = GetAngleFromTriangle(rightUpperArmLength, rightForearmLength, lengthToTarget);
            if (!double.IsNaN(x))
                forearm.localRotation = Quaternion.Euler(180 - x, 0.0f, 0.0f);
        }
        m_RightArm[HandIndex].rotation = m_RightTarget.rotation;
    }

    private static float GetAngleFromTriangle(float a, float b, float c)
    {
        return Mathf.Rad2Deg * Mathf.Acos((Mathf.Pow(a, 2) + Mathf.Pow(b, 2) - Mathf.Pow(c, 2)) / (2 * a * b));
    }
}