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

    private static void HandleArm(Arm arm)
    {
        Transform target = arm.target;    
        if (!SetupArm(arm) || target == null) return; 
        Transform[] bones = arm.bones;
        float rightForearmLength = Vector3.Distance(bones[HandIndex].position, bones[ForearmIndex].position),
              rightUpperArmLength = Vector3.Distance(bones[ForearmIndex].position, bones[UpperArmIndex].position),
              lengthToTarget = Vector3.Distance(target.position, bones[UpperArmIndex].position);
        {
            Transform upperArm = bones[UpperArmIndex];
            Vector3 direction = target.position - upperArm.position;
            float z = Mathf.Rad2Deg * Mathf.Atan2(-direction.x, -direction.z);
            float x = GetAngleFromTriangle(rightUpperArmLength, lengthToTarget, rightForearmLength);
            // x += Mathf.Rad2Deg * Mathf.Atan2(-direction.y, direction.z);
            x += Mathf.Rad2Deg * Mathf.Atan2(-direction.y, Vector3.Distance(upperArm.position, new Vector3 {x = target.position.x, y = upperArm.position.y, z = target.position.z}));
            if (!double.IsNaN(x) && !double.IsNaN(z))
                upperArm.localRotation = Quaternion.Euler(0.0f, 0.0f, z) * Quaternion.Euler(360.0f - x, 0.0f, 0.0f);
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
        HandleArm(m_Left);
        HandleArm(m_Right);
    }

    private static float GetAngleFromTriangle(float a, float b, float c)
    {
        return Mathf.Rad2Deg * Mathf.Acos((Mathf.Pow(a, 2) + Mathf.Pow(b, 2) - Mathf.Pow(c, 2)) / (2 * a * b));
    }
}