using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Conversion
{
    public static System.Numerics.Vector3 FromUnityVector3(Vector3 source)
    {
        System.Numerics.Vector3 me;
        me.X = source.x;
        me.Y = source.y;
        me.Z = source.z;
        return me;
    }

    public static System.Numerics.Quaternion FromUnityQuaternion(Quaternion source)
    {
        System.Numerics.Quaternion me;
        me.X = source.x;
        me.Y = source.y;
        me.Z = source.z;
        me.W = source.w;
        return me;
    }

    public static Vector3 FromNumericsVector3(System.Numerics.Vector3 source)
    {
        Vector3 me;
        me.x = source.X;
        me.y = source.Y;
        me.z = source.Z;
        return me;
    }

    public static Quaternion FromNumericsQuaternion(System.Numerics.Quaternion source)
    {
        Quaternion me;
        me.x = source.X;
        me.y = source.Y;
        me.z = source.Z;
        me.w = source.W;
        return me;
    }

    public static Pose GetRelativePose(Vector3 position, Quaternion rotation, Vector3 refPosition, Quaternion refRotation, Vector3 refScale)
    {
        Quaternion outputRotation = GetRelativeRotation(rotation, refRotation);
        Vector3 outputPosition = GetRelativePosition(position, refPosition, refRotation, refScale);

        return new Pose(outputPosition, outputRotation);
    }

    public static Vector3 GetRelativePosition(Vector3 position, Vector3 refPosition, Quaternion refRotation, Vector3 refScale)
    {
        Vector3 outputPosition = (Quaternion.Inverse(refRotation) * (position - refPosition));

        if (refScale != Vector3.zero)
        {
            outputPosition.x /= refScale.x;
            outputPosition.y /= refScale.y;
            outputPosition.z /= refScale.z;
        }

        return outputPosition;
    }

    public static Quaternion GetRelativeRotation(Quaternion rotation, Quaternion refRotation)
    {
        Quaternion outputRotation = Quaternion.Inverse(refRotation) * rotation;

        return outputRotation;
    }

    public static Pose GetRelativePose(this Transform me, Pose pose, Vector3 scale = default)
    {
        return GetRelativePose(me.position, me.rotation, pose.position, pose.rotation, scale);
    }

    public static Pose GetRelativePose(this Transform me, Transform transform)
    {
        return GetRelativePose(me.position, me.rotation, transform.position, transform.rotation, transform.localScale);
    }

    public static Vector3 GetRelativePosition(this Transform me, Transform transform)
    {
        return GetRelativePosition(me.position, transform.position, transform.rotation, transform.localScale);
    }

    public static Quaternion GetRelativeRotation(this Transform me, Transform transform)
    {
        return GetRelativeRotation(me.rotation, transform.rotation);
    }

    public static Pose GetRelativePose(this Pose me, Pose pose, Vector3 scale = default)
    {
        return GetRelativePose(me.position, me.rotation, pose.position, pose.rotation, scale);
    }

    public static Pose GetRelativePose(this Pose me, Transform transform)
    {
        return GetRelativePose(me.position, me.rotation, transform.position, transform.rotation, transform.localScale);
    }

    public static Vector3 GetRelativePosition(this Pose me, Transform transform)
    {
        return GetRelativePosition(me.position, transform.position, transform.rotation, transform.localScale);
    }

    public static Quaternion GetRelativeRotation(this Pose me, Transform transform)
    {
        return GetRelativeRotation(me.rotation, transform.rotation);
    }



    public static Pose GetAbsolutePose(Vector3 position, Quaternion rotation, Vector3 refPosition, Quaternion refRotation, Vector3 refScale)
    {
        Quaternion outputRotation = GetAbsoluteRotation(rotation, refRotation);
        Vector3 outputPosition = GetAbsolutePosition(position, refPosition, refRotation, refScale);

        return new Pose(outputPosition, outputRotation);
    }

    public static Vector3 GetAbsolutePosition(Vector3 position, Vector3 refPosition, Quaternion refRotation, Vector3 refScale)
    {
        Vector3 scaledPosition = position;

        if (refScale != Vector3.zero)
        {
            position.x *= refScale.x;
            position.y *= refScale.y;
            position.z *= refScale.z;
        }

        Vector3 outputPosition = (refRotation * position) + refPosition;

        return outputPosition;
    }

    public static Quaternion GetAbsoluteRotation(Quaternion rotation, Quaternion refRotation)
    {
        Quaternion outputRotation = refRotation * rotation;

        return outputRotation;
    }

    public static Pose GetAbsolutePose(this Transform me, Pose pose, Vector3 scale = default)
    {
        return GetAbsolutePose(me.position, me.rotation, pose.position, pose.rotation, scale);
    }

    public static Pose GetAbsolutePose(this Transform me, Transform transform)
    {
        return GetAbsolutePose(me.position, me.rotation, transform.position, transform.rotation, transform.localScale);
    }

    public static Vector3 GetAbsolutePosition(this Transform me, Transform transform)
    {
        return GetAbsolutePosition(me.position, transform.position, transform.rotation, transform.localScale);
    }

    public static Quaternion GetAbsoluteRotation(this Transform me, Transform transform)
    {
        return GetAbsoluteRotation(me.rotation, transform.rotation);
    }

    public static Pose GetAbsolutePose(this Pose me, Pose pose, Vector3 scale = default)
    {
        return GetAbsolutePose(me.position, me.rotation, pose.position, pose.rotation, scale);
    }

    public static Pose GetAbsolutePose(this Pose me, Transform transform)
    {
        return GetAbsolutePose(me.position, me.rotation, transform.position, transform.rotation, transform.localScale);
    }

    public static Vector3 GetAbsolutePosition(this Pose me, Transform transform)
    {
        return GetAbsolutePosition(me.position, transform.position, transform.rotation, transform.localScale);
    }

    public static Quaternion GetAbsoluteRotation(this Pose me, Transform transform)
    {
        return GetAbsoluteRotation(me.rotation, transform.rotation);
    }
}
