using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body
{
    public Dictionary<int, Joint> Joints { get; private set; }
    public int Id { get; private set; }

    public Body(int id, Dictionary<int, Joint> joints)
    {
        Joints = joints;
        Id = id;
    }

    public struct Joint
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public int Id;
        public Confidence Confidence;
    }
    public enum Confidence
    {
        //
        // Zusammenfassung:
        //     The joint is out of range (too far from depth camera)
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
