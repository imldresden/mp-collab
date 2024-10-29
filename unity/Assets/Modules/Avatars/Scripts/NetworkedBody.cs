using System.Runtime.InteropServices;

// Class with relevant information about body
// bodyId and 2d and 3d points of all joints
public struct NetworkedBody
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public System.Numerics.Vector3[] JointPositions3D;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public System.Numerics.Vector2[] JointPositions2D;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public System.Numerics.Quaternion[] JointRotations;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public JointConfidenceLevel[] JointPrecisions;

    public int Length;

    public uint Id;

    public NetworkedBody(int maxJointsLength)
    {
        JointPositions3D = new System.Numerics.Vector3[maxJointsLength];
        JointPositions2D = new System.Numerics.Vector2[maxJointsLength];
        JointRotations = new System.Numerics.Quaternion[maxJointsLength];
        JointPrecisions = new JointConfidenceLevel[maxJointsLength];

        Length = 0;
        Id = 0;
    }

    public static NetworkedBody DeepCopy(NetworkedBody copyFromBody)
    {
        int maxJointsLength = copyFromBody.Length;
        NetworkedBody copiedBody = new NetworkedBody(maxJointsLength);

        for (int i = 0; i < maxJointsLength; i++)
        {
            copiedBody.JointPositions2D[i] = copyFromBody.JointPositions2D[i];
            copiedBody.JointPositions3D[i] = copyFromBody.JointPositions3D[i];
            copiedBody.JointRotations[i] = copyFromBody.JointRotations[i];
            copiedBody.JointPrecisions[i] = copyFromBody.JointPrecisions[i];
        }
        copiedBody.Id = copyFromBody.Id;
        copiedBody.Length = copyFromBody.Length;
        return copiedBody;
    }

    public enum JointConfidenceLevel
    {
        None = 0,
        //
        // Zusammenfassung:
        //     The joint is not observed (likely due to occlusion), predicted joint pose
        Low = 1,
        //
        // Zusammenfassung:
        //     Medium confidence in joint pose. Current SDK will only provide joints up to this
        //     confidence level
        Medium = 2,
        //
        // Zusammenfassung:
        //     High confidence in joint pose. Placeholder for future SDK
        High = 3,
        //
        // Zusammenfassung:
        //     The total number of confidence levels.
        Count = 4
    }
}

