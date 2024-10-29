using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IMLD.MixedReality.Core;

namespace IMLD.MixedReality.UI
{
    public class SimplePlaybackUI : MonoBehaviour
    {
        [SerializeField] private PlaybackControl _playbackControl;

        // Start is called before the first frame update
        void Start()
        {

        }

        private void OnGUI()
        {
            if (_playbackControl == null)
            {
                return;
            }
            
            if (_playbackControl.TimelineStatus == TimelineStatus.PLAYING)
            {
                GUI.Label(new Rect(10, 75, 200, 30), "PLAYING: " + (int)(_playbackControl.Progress * 100) + "%");
                if (GUI.Button(new Rect(10, 110, 200, 60), "Pause"))
                {
                    _playbackControl.PausePlayback();
                }
            }
            else
            {
                GUI.Label(new Rect(10, 75, 200, 30), "PAUSED: " + (int)(_playbackControl.Progress * 100) + "%");
                if (GUI.Button(new Rect(10, 110, 200, 60), "Play"))
                {
                    _playbackControl.ResumePlayback();
                }
            }
        }
    }
}