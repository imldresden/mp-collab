using Microsoft.Azure.Kinect.Sensor;

public static class AzureExtensionsMethods
{
    public static void CopyFromBodyTrackingSdk(this ref NetworkedBody me, Microsoft.Azure.Kinect.BodyTracking.Body body, Calibration sensorCalibration)
    {
        me.Id = body.Id;
        me.Length = Microsoft.Azure.Kinect.BodyTracking.Skeleton.JointCount;

        for (int bodyPoint = 0; bodyPoint < me.Length; bodyPoint++)
        {
            // K4ABT joint position unit is in millimeter. We need to convert to meters before we use the values.
            me.JointPositions3D[bodyPoint] = body.Skeleton.GetJoint(bodyPoint).Position / 1000.0f;
            me.JointRotations[bodyPoint] = body.Skeleton.GetJoint(bodyPoint).Quaternion;
            me.JointPrecisions[bodyPoint] = (NetworkedBody.JointConfidenceLevel)body.Skeleton.GetJoint(bodyPoint).ConfidenceLevel;

            var jointPosition = me.JointPositions3D[bodyPoint];
            var position2d = sensorCalibration.TransformTo2D(
                jointPosition,
                CalibrationDeviceType.Depth,
                CalibrationDeviceType.Depth);

            if (position2d != null)
            {
                me.JointPositions2D[bodyPoint] = position2d.Value;
            }
            else
            {
                me.JointPositions2D[bodyPoint].X = Constants.Invalid2DCoordinate;
                me.JointPositions2D[bodyPoint].Y = Constants.Invalid2DCoordinate;
            }
        }
    }
}
