using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Microsoft.MixedReality.Toolkit.Utilities;
using IMLD.MixedReality.Core;
using System;

namespace IMLD.MixedReality.Avatars
{
    public class SimpleAvatar : AbstractAvatar
    {
        HandDataFrame LeftHand, RightHand;

        [SerializeField] private CustomNetworkedHandVisualizer _handVisualizerLeft;
        [SerializeField] private CustomNetworkedHandVisualizer _handVisualizerRight;
        [SerializeField] private GameObject _leftHandRig;
        [SerializeField] private GameObject _rightHandRig;

        public override void ApplyHandPosture(HandDataFrame leftHand, HandDataFrame rightHand)
        {
            LeftHand = leftHand;
            RightHand = rightHand;
        }

        private void Start()
        {

        }

        // Update is called once per frame
        private void LateUpdate()
        {
            // Update hand data
            if(_leftHandRig != null)
            {
                if (_handVisualizerLeft != null && LeftHand.JointRotations != null)
                {
                    _leftHandRig.SetActive(true);
                    _handVisualizerLeft.ApplyHandPosture(LeftHand);
                }
                else
                {
                    _leftHandRig.SetActive(false);
                }
            }


            if (_rightHandRig != null)
            {
                if (_handVisualizerRight != null && RightHand.JointRotations != null)
                {
                    _rightHandRig.SetActive(true);
                    _handVisualizerRight.ApplyHandPosture(RightHand);
                }
                else
                {
                    _rightHandRig.SetActive(false);
                }
            }
        }

    }
}