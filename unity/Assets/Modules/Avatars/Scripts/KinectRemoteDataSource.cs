using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace IMLD.MixedReality.Avatars
{
    public class KinectRemoteDataSource : MonoBehaviour, IBodyDataSource, IPointCloudSource
    {
        
        public Dictionary<CustomJointId, CustomJointId> parentJointMap;
        public Quaternion[] absoluteJointRotations = new Quaternion[(int)CustomJointId.Count];

        [SerializeField] private PointCloudManager _pointCloudManager;


        private Dictionary<CustomJointId, Quaternion> _basisJointMap;
        private Quaternion Y_180_FLIP = new Quaternion(0.0f, 1.0f, 0.0f, 0.0f);
        private Dictionary<int, Body> _latestSkeletons = new Dictionary<int, Body>();
        private PointCloudDataFrame _latestPointCloud;
        private INetworkServiceManager _networkServiceManager;
        private INetworkService _kinectDataService;

        IReadOnlyList<Body> IBodyDataSource.Bodies { get { return _latestSkeletons.Values.ToList(); } }

        PointCloudDataFrame IPointCloudSource.PointCloud { get { return _latestPointCloud; } }
        public bool RenderPointClouds
        {
            get
            {
                if (_pointCloudManager != null)
                {
                    return _pointCloudManager.enabled;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                if (_pointCloudManager != null)
                {
                    _pointCloudManager.enabled = value;
                }
            }
        }
        int IBodyDataSource.NumOfBodies { get { return _latestSkeletons.Count; } }

        public Guid SourceId { get; set; }

        public void UpdateSkeletonData(SkeletonDataFrame data)
        {
            // update skeleton data
            _latestSkeletons.Clear();
            foreach (var item in data.Bodies)
            {
                if (item.Length == (int)CustomJointId.Count)
                {
                    _latestSkeletons[(int)item.Id] = CreateBodyFromData(item);
                }
            }
        }

        public void UpdatePointCloudData(PointCloudDataFrame data)
        {
            // update point cloud data
            _latestPointCloud = data;
        }

        void Awake()
        {
            parentJointMap = new Dictionary<CustomJointId, CustomJointId>();

            // pelvis has no parent so set to count
            parentJointMap[CustomJointId.Pelvis] = CustomJointId.Count;
            parentJointMap[CustomJointId.SpineNavel] = CustomJointId.Pelvis;
            parentJointMap[CustomJointId.SpineChest] = CustomJointId.SpineNavel;
            parentJointMap[CustomJointId.Neck] = CustomJointId.SpineChest;
            parentJointMap[CustomJointId.ClavicleLeft] = CustomJointId.SpineChest;
            parentJointMap[CustomJointId.ShoulderLeft] = CustomJointId.ClavicleLeft;
            parentJointMap[CustomJointId.ElbowLeft] = CustomJointId.ShoulderLeft;
            parentJointMap[CustomJointId.WristLeft] = CustomJointId.ElbowLeft;
            parentJointMap[CustomJointId.HandLeft] = CustomJointId.WristLeft;
            parentJointMap[CustomJointId.HandTipLeft] = CustomJointId.HandLeft;
            parentJointMap[CustomJointId.ThumbLeft] = CustomJointId.HandLeft;
            parentJointMap[CustomJointId.ClavicleRight] = CustomJointId.SpineChest;
            parentJointMap[CustomJointId.ShoulderRight] = CustomJointId.ClavicleRight;
            parentJointMap[CustomJointId.ElbowRight] = CustomJointId.ShoulderRight;
            parentJointMap[CustomJointId.WristRight] = CustomJointId.ElbowRight;
            parentJointMap[CustomJointId.HandRight] = CustomJointId.WristRight;
            parentJointMap[CustomJointId.HandTipRight] = CustomJointId.HandRight;
            parentJointMap[CustomJointId.ThumbRight] = CustomJointId.HandRight;
            parentJointMap[CustomJointId.HipLeft] = CustomJointId.SpineNavel;
            parentJointMap[CustomJointId.KneeLeft] = CustomJointId.HipLeft;
            parentJointMap[CustomJointId.AnkleLeft] = CustomJointId.KneeLeft;
            parentJointMap[CustomJointId.FootLeft] = CustomJointId.AnkleLeft;
            parentJointMap[CustomJointId.HipRight] = CustomJointId.SpineNavel;
            parentJointMap[CustomJointId.KneeRight] = CustomJointId.HipRight;
            parentJointMap[CustomJointId.AnkleRight] = CustomJointId.KneeRight;
            parentJointMap[CustomJointId.FootRight] = CustomJointId.AnkleRight;
            parentJointMap[CustomJointId.Head] = CustomJointId.Pelvis;
            parentJointMap[CustomJointId.Nose] = CustomJointId.Head;
            parentJointMap[CustomJointId.EyeLeft] = CustomJointId.Head;
            parentJointMap[CustomJointId.EarLeft] = CustomJointId.Head;
            parentJointMap[CustomJointId.EyeRight] = CustomJointId.Head;
            parentJointMap[CustomJointId.EarRight] = CustomJointId.Head;

            Vector3 zpositive = Vector3.forward;
            Vector3 xpositive = Vector3.right;
            Vector3 ypositive = Vector3.up;
            // spine and left hip are the same
            Quaternion leftHipBasis = Quaternion.LookRotation(xpositive, -zpositive);
            Quaternion spineHipBasis = Quaternion.LookRotation(xpositive, -zpositive);
            Quaternion rightHipBasis = Quaternion.LookRotation(xpositive, zpositive);
            // arms and thumbs share the same basis
            Quaternion leftArmBasis = Quaternion.LookRotation(ypositive, -zpositive);
            Quaternion rightArmBasis = Quaternion.LookRotation(-ypositive, zpositive);
            Quaternion leftHandBasis = Quaternion.LookRotation(-zpositive, -ypositive);
            Quaternion rightHandBasis = Quaternion.identity;
            Quaternion leftFootBasis = Quaternion.LookRotation(xpositive, ypositive);
            Quaternion rightFootBasis = Quaternion.LookRotation(xpositive, -ypositive);

            _basisJointMap = new Dictionary<CustomJointId, Quaternion>();

            // pelvis has no parent so set to count
            _basisJointMap[CustomJointId.Pelvis] = spineHipBasis;
            _basisJointMap[CustomJointId.SpineNavel] = spineHipBasis;
            _basisJointMap[CustomJointId.SpineChest] = spineHipBasis;
            _basisJointMap[CustomJointId.Neck] = spineHipBasis;
            _basisJointMap[CustomJointId.ClavicleLeft] = leftArmBasis;
            _basisJointMap[CustomJointId.ShoulderLeft] = leftArmBasis;
            _basisJointMap[CustomJointId.ElbowLeft] = leftArmBasis;
            _basisJointMap[CustomJointId.WristLeft] = leftHandBasis;
            _basisJointMap[CustomJointId.HandLeft] = leftHandBasis;
            _basisJointMap[CustomJointId.HandTipLeft] = leftHandBasis;
            _basisJointMap[CustomJointId.ThumbLeft] = leftArmBasis;
            _basisJointMap[CustomJointId.ClavicleRight] = rightArmBasis;
            _basisJointMap[CustomJointId.ShoulderRight] = rightArmBasis;
            _basisJointMap[CustomJointId.ElbowRight] = rightArmBasis;
            _basisJointMap[CustomJointId.WristRight] = rightHandBasis;
            _basisJointMap[CustomJointId.HandRight] = rightHandBasis;
            _basisJointMap[CustomJointId.HandTipRight] = rightHandBasis;
            _basisJointMap[CustomJointId.ThumbRight] = rightArmBasis;
            _basisJointMap[CustomJointId.HipLeft] = leftHipBasis;
            _basisJointMap[CustomJointId.KneeLeft] = leftHipBasis;
            _basisJointMap[CustomJointId.AnkleLeft] = leftHipBasis;
            _basisJointMap[CustomJointId.FootLeft] = leftFootBasis;
            _basisJointMap[CustomJointId.HipRight] = rightHipBasis;
            _basisJointMap[CustomJointId.KneeRight] = rightHipBasis;
            _basisJointMap[CustomJointId.AnkleRight] = rightHipBasis;
            _basisJointMap[CustomJointId.FootRight] = rightFootBasis;
            _basisJointMap[CustomJointId.Head] = spineHipBasis;
            _basisJointMap[CustomJointId.Nose] = spineHipBasis;
            _basisJointMap[CustomJointId.EyeLeft] = spineHipBasis;
            _basisJointMap[CustomJointId.EarLeft] = spineHipBasis;
            _basisJointMap[CustomJointId.EyeRight] = spineHipBasis;
            _basisJointMap[CustomJointId.EarRight] = spineHipBasis;
        }

        void Start()
        {
            if (_pointCloudManager != null)
            {
                _pointCloudManager.PointCloudSource = this;
            }
        }
        Body IBodyDataSource.GetBody(int skeletonId)
        {
            try
            {
                return _latestSkeletons[skeletonId];
            }
            catch (KeyNotFoundException)
            {
                Debug.LogWarning("Cannot retrieve skeleton data. Key not found: " + skeletonId);
                return null;
            }
        }

        Body IBodyDataSource.GetClosestBody(Transform transform)
        {
            if (_latestSkeletons == null || _latestSkeletons.Count == 0)
            {
                return null;
            }

            var position = Conversion.GetRelativePosition(transform, this.transform);

            int closestSkeleton = -1;
            float minDistance = float.MaxValue;
            foreach (var kvp in _latestSkeletons)
            {
                var headPosition = kvp.Value.Joints[(int)CustomJointId.Head].Position; // get current head position from body
                float distance = Vector3.SqrMagnitude(headPosition - position);
                if (distance < minDistance)
                {
                    closestSkeleton = kvp.Key;
                    minDistance = distance;
                }

                //Debug.Log("HMD: " + position + ", Head: " + headPosition + ", Sqr. Distance: " + distance);
            }

            bool result = _latestSkeletons.TryGetValue(closestSkeleton, out Body closestBody);

            if (result == true)
            {
                return closestBody;
            }
            else
            {
                return null;
            }

        }

        string IBodyDataSource.GetJointName(int jointId)
        {
            return Enum.GetName(typeof(CustomJointId), jointId);
        }

        int IBodyDataSource.GetJointParent(int jointId)
        {
            return (int)parentJointMap[(CustomJointId)jointId];
        }



        private Body CreateBodyFromData(NetworkedBody body)
        {
            Dictionary<int, Body.Joint> JointDict = new Dictionary<int, Body.Joint>();

            for (int jointNum = 0; jointNum < (int)CustomJointId.Count; jointNum++)
            {
                Body.Joint Joint = new Body.Joint();
                Joint.Position = new Vector3(body.JointPositions3D[jointNum].X, -body.JointPositions3D[jointNum].Y, body.JointPositions3D[jointNum].Z);
                Joint.Rotation = Y_180_FLIP * new Quaternion(body.JointRotations[jointNum].X, body.JointRotations[jointNum].Y,
                    body.JointRotations[jointNum].Z, body.JointRotations[jointNum].W) * Quaternion.Inverse(_basisJointMap[(CustomJointId)jointNum]);
                Joint.Id = jointNum;
                Joint.Confidence = (Body.Confidence)body.JointPrecisions[jointNum];
                JointDict.Add(Joint.Id, Joint);
            }

            Body Body = new Body((int)body.Id, JointDict);
            return Body;
        }
    }

    public enum CustomJointId
    {
        //
        // Zusammenfassung:
        //     Pelvis
        Pelvis = 0,
        //
        // Zusammenfassung:
        //     Spine navel
        SpineNavel = 1,
        //
        // Zusammenfassung:
        //     Spine chest
        SpineChest = 2,
        //
        // Zusammenfassung:
        //     Neck
        Neck = 3,
        //
        // Zusammenfassung:
        //     Left clavicle
        ClavicleLeft = 4,
        //
        // Zusammenfassung:
        //     Left shoulder
        ShoulderLeft = 5,
        //
        // Zusammenfassung:
        //     Left elbow
        ElbowLeft = 6,
        //
        // Zusammenfassung:
        //     Left wrist
        WristLeft = 7,
        //
        // Zusammenfassung:
        //     Left hand
        HandLeft = 8,
        //
        // Zusammenfassung:
        //     Left hand tip
        HandTipLeft = 9,
        //
        // Zusammenfassung:
        //     Left thumb
        ThumbLeft = 10,
        //
        // Zusammenfassung:
        //     Right clavicle
        ClavicleRight = 11,
        //
        // Zusammenfassung:
        //     Right shoulder
        ShoulderRight = 12,
        //
        // Zusammenfassung:
        //     Right elbow
        ElbowRight = 13,
        //
        // Zusammenfassung:
        //     Right wrist
        WristRight = 14,
        //
        // Zusammenfassung:
        //     Right hand
        HandRight = 15,
        //
        // Zusammenfassung:
        //     Right hand tip
        HandTipRight = 16,
        //
        // Zusammenfassung:
        //     Right thumb
        ThumbRight = 17,
        //
        // Zusammenfassung:
        //     Left hip
        HipLeft = 18,
        //
        // Zusammenfassung:
        //     Left knee
        KneeLeft = 19,
        //
        // Zusammenfassung:
        //     Left ankle
        AnkleLeft = 20,
        //
        // Zusammenfassung:
        //     Left foot
        FootLeft = 21,
        //
        // Zusammenfassung:
        //     Right hip
        HipRight = 22,
        //
        // Zusammenfassung:
        //     Right knee
        KneeRight = 23,
        //
        // Zusammenfassung:
        //     Right ankle
        AnkleRight = 24,
        //
        // Zusammenfassung:
        //     Right foot
        FootRight = 25,
        //
        // Zusammenfassung:
        //     Head
        Head = 26,
        //
        // Zusammenfassung:
        //     Nose
        Nose = 27,
        //
        // Zusammenfassung:
        //     Left eye
        EyeLeft = 28,
        //
        // Zusammenfassung:
        //     Left ear
        EarLeft = 29,
        //
        // Zusammenfassung:
        //     Right eye
        EyeRight = 30,
        //
        // Zusammenfassung:
        //     Right ear
        EarRight = 31,
        //
        // Zusammenfassung:
        //     Number of different joints defined in this enumeration.
        Count = 32
    }
}