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
        float forearmLength = Vector3.Distance(bones[HandIndex].position, bones[ForearmIndex].position),
              upperArmLength = Vector3.Distance(bones[ForearmIndex].position, bones[UpperArmIndex].position),
              distanceToTarget = Vector3.Distance(targetPosition, bones[UpperArmIndex].position);
        {
            Transform upperArm = bones[UpperArmIndex];
            Vector3 upperArmPosition = upperArm.position,
                    directionToTarget = targetPosition - upperArmPosition;
            float upperArmAngle = GetAngleFromTriangle(upperArmLength, distanceToTarget, forearmLength);
            if (!double.IsNaN(upperArmAngle))
                upperArm.rotation = Quaternion.LookRotation(directionToTarget) * Quaternion.AngleAxis(upperArmAngle, Vector3.right);
        }
        {
            Transform forearm = bones[ForearmIndex];
            float x = GetAngleFromTriangle(upperArmLength, forearmLength, distanceToTarget);
            if (!double.IsNaN(x))
                forearm.localRotation = Quaternion.AngleAxis(x + 180.0f, Vector3.right);
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
        return Mathf.Rad2Deg * Mathf.Acos((a * a + b * b - c * c) / (2 * a * b));
    }
}