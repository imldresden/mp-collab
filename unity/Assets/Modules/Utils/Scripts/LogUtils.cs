using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class LogUtils
{
    public static string ToString(Transform transform, string delim = "\t")
    {
        StringBuilder buffer = new StringBuilder();

        if (transform == null)
        {
            buffer.Append(float.NaN);
            buffer.Append(delim);
            buffer.Append(float.NaN);
            buffer.Append(delim);
            buffer.Append(float.NaN);
            buffer.Append(delim);
            buffer.Append(float.NaN);
            buffer.Append(delim);
            buffer.Append(float.NaN);
            buffer.Append(delim);
            buffer.Append(float.NaN);
            buffer.Append(delim);
            buffer.Append(float.NaN);
            return buffer.ToString();
        }

        buffer.Append(transform.position.x);
        buffer.Append(delim);
        buffer.Append(transform.position.y);
        buffer.Append(delim);
        buffer.Append(transform.position.z);
        buffer.Append(delim);
        buffer.Append(transform.rotation.w);
        buffer.Append(delim);
        buffer.Append(transform.rotation.x);
        buffer.Append(delim);
        buffer.Append(transform.rotation.y);
        buffer.Append(delim);
        buffer.Append(transform.rotation.z);
        return buffer.ToString();
    }

    public static string ToString(Vector3 vector, string delim = "\t")
    {
        StringBuilder buffer = new StringBuilder();
        buffer.Append(vector.x);
        buffer.Append(delim);
        buffer.Append(vector.y);
        buffer.Append(delim);
        buffer.Append(vector.z);
        return buffer.ToString();
    }

    public static string ToString(Quaternion quat, string delim ="\t")
    {
        StringBuilder buffer = new StringBuilder();
        buffer.Append(quat.w);
        buffer.Append(delim);
        buffer.Append(quat.x);
        buffer.Append(delim);
        buffer.Append(quat.y);
        buffer.Append(delim);
        buffer.Append(quat.z);
        return buffer.ToString();
    }
}
