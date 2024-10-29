using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Core
{
    public class PlaybackControl : MonoBehaviour
    {
        public TimelineStatus TimelineStatus { get; private set; } = TimelineStatus.PAUSED;
        public long AbsoluteTimestamp { get { return FirstTimestamp + RelativeTimestamp; } }
        public long RelativeTimestamp { get; private set; } = 0L;

        public float Progress { get { return RelativeTimestamp / (float)(LastTimestamp -  FirstTimestamp); } }
        public float PlaybackSpeed { get; private set; } = 1.0f;

        public long FirstTimestamp
        {
            get
            {
                return currentTimeFilterMin;
            }

            set
            {
                currentTimeFilterMin = value;
            }
        }

        public long LastTimestamp
        {
            get
            {
                return currentTimeFilterMax;
            }

            set
            {
                currentTimeFilterMax = value;
            }
        }

        private long currentTimeFilterMax = long.MinValue;
        private long currentTimeFilterMin = long.MaxValue;

        private const long TICKS_PER_SECOND = 10000000;


        // Update is called once per frame
        void Update()
        {
            // update time stamp if necessary
            if (TimelineStatus == TimelineStatus.PLAYING)
            {
                RelativeTimestamp += (long)(Time.deltaTime * TICKS_PER_SECOND * PlaybackSpeed);
                if (AbsoluteTimestamp >= currentTimeFilterMax)
                {
                    PausePlayback(); // pause/stop playback if timeline has reached its end
                }
            }
        }

        public long PausePlayback()
        {
            TimelineStatus = TimelineStatus.PAUSED;
            return AbsoluteTimestamp;
        }

        public bool ResumePlayback()
        {
            if (AbsoluteTimestamp < currentTimeFilterMax)
            {
                TimelineStatus = TimelineStatus.PLAYING;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// An enum representing the current status of the timeline.
    /// </summary>
    public enum TimelineStatus
    {
        /// <summary>
        /// Playback is running.
        /// </summary>
        PLAYING,

        /// <summary>
        /// Playback is paused.
        /// </summary>
        PAUSED
    }
}