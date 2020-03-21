using System;
using UnityEngine;

[ExecuteInEditMode]
public class ArmIk : MonoBehaviour
{
    [Serializable]
    private class Arm
    {
        public Transform hand, target;
        [NonSerialized] public Transform[] bones;
    }

    private const int BoneCount = 3, HandIndex = 0, ForearmIndex = 1, UpperArmIndex = 2;

    [SerializeField] private Arm m_Left, m_Right;

    private static bool SetupArm(Arm arm)
    {
        Transform hand = arm.hand;
        if (hand == null) return false;
        Transform current = hand;
        for (var boneIndex = 0; boneIndex < BoneCount; boneIndex++)
        {
            if (current == null) return false;
            if (arm.bones == null) arm.bones = new Transform[BoneCount];
            arm.bones[boneIndex] = current;
            current = current.parent;
        }
        return true;
    }

    private static void PositionArm(Arm arm)
    {
        Transform target = arm.target;
        if (!SetupArm(arm) || target == null) return;
        Transform[] bones = arm.bones;
        Vector3 targetPosition = target.position;
        float rightForearmLength = Vector3.Distance(bones[HandIndex].position, bones[ForearmIndex].position),
              rightUpperArmLength = Vector3.Distance(bones[ForearmIndex].position, bones[UpperArmIndex].position),
              lengthToTarget = Vector3.Distance(targetPosition, bones[UpperArmIndex].position);
        {
            Transform upperArm = bones[UpperArmIndex];
            Vector3 upperArmPosition = upperArm.position,
                    directionToTarget = targetPosition - upperArmPosition;
            float lateralAngleToTarget = Mathf.Rad2Deg * Mathf.Atan2(-directionToTarget.x, -directionToTarget.z),
                  lateralDistanceToTarget = Vector3.Distance(upperArmPosition, new Vector3 {x = targetPosition.x, y = upperArmPosition.y, z = targetPosition.z}),
                  elevationAngleToTarget = Mathf.Rad2Deg * Mathf.Atan2(-directionToTarget.y, lateralDistanceToTarget),
                  upperArmAngle = GetAngleFromTriangle(rightUpperArmLength, lengthToTarget, rightForearmLength) + elevationAngleToTarget;
            if (!double.IsNaN(upperArmAngle) && !double.IsNaN(lateralAngleToTarget))
                // Order is important - first look towards target in xz plane and then rotate
                upperArm.localRotation = Quaternion.Euler(0.0f, 0.0f, lateralAngleToTarget) * Quaternion.Euler(360.0f - upperArmAngle, 0.0f, 0.0f);
        }
        {
            Transform forearm = bones[ForearmIndex];
            float x = GetAngleFromTriangle(rightUpperArmLength, rightForearmLength, lengthToTarget);
            if (!double.IsNaN(x))
                forearm.localRotation = Quaternion.Euler(180.0f - x, 0.0f, 0.0f);
        }
        bones[HandIndex].rotation = target.rotation;
    }

    private void LateUpdate()
    {
        PositionArm(m_Left);
        PositionArm(m_Right);
    }

    private static float GetAngleFromTriangle(float a, float b, float c)
    {
        // Law of cosines to find angle given three known side lengths
        return Mathf.Rad2Deg * Mathf.Acos((Mathf.Pow(a, 2) + Mathf.Pow(b, 2) - Mathf.Pow(c, 2)) / (2 * a * b));
    }
}