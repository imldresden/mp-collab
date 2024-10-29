using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// This class computes a transformation between two coordinate systems based on sets of poses over time.
/// It supports different update rates and, to some degree, missing poses (e.g., if tracking was temporarily lost).
/// </summary>
public class DelayedRegistrationRefiner : MonoBehaviour
{
    /// <summary>
    /// The size of the search window for the time offset between the sets of poses in seconds.
    /// I.e., the largest assumed time offset between matching poses.
    /// Must not be larger than the window size.
    /// </summary>
    [SerializeField] private float _maxDelay = 0.2f;

    /// <summary>
    /// The buffer size for the sample buffers.
    /// </summary>
    [SerializeField] private int _bufferSize = 120;

    /// <summary>
    /// The target sampling rate for the poses. Data will be resampled to roughly match this rate.
    /// </summary>
    [SerializeField] private int _samplingRate = 60;

    /// <summary>
    /// The length in seconds of the pose sample window.
    /// </summary>
    [SerializeField] private float _windowSize = 5.0f;

    /// <summary>
    /// The maximum time in seconds that tracking of any pose may temporarily get lost. Should be much smaller than the window size.
    /// </summary>
    [SerializeField] private float _maxTrackingLostTime = 0.1f;

    private RingBuffer<TimedPosition> _poseBufferReference;
    private RingBuffer<TimedPosition> _poseBufferExternal;

    private List<TimedPosition> _poseListReferenceNormalized;
    private List<TimedPosition> _poseListExternalNormalized;

    private bool _referenceBufferReady = false;
    private bool _externalBufferReady = false;

    private Vector3 _prevRefPos = Vector3.zero;
    private Vector3 _prevExtPos = Vector3.zero;

    public void AddReferencePosition(Vector3 position)
    {
        if (position == _prevRefPos) { return; }
        _prevRefPos = position;
        var poseTime = DateTimeOffset.UtcNow;

        // add new element
        _poseBufferReference.Put(new TimedPosition(position, poseTime));

        var referenceStartTime = GetBufferStartTime(_poseBufferReference);
        if ((poseTime - referenceStartTime).TotalSeconds >= _windowSize)
        {
            _referenceBufferReady = true;
        }
    }

    public void AddExternalPosition(Vector3 position)
    {
        if (position == _prevExtPos) { return; }
        _prevExtPos = position;
        var poseTime = DateTimeOffset.UtcNow;        

        _poseBufferExternal.Put(new TimedPosition(position, poseTime));

        var externalStartTime = GetBufferStartTime(_poseBufferExternal);

        if ((poseTime - externalStartTime).TotalSeconds >= _windowSize)
        {
            _externalBufferReady = true;
        }
    }

    private void LateUpdate()
    {
        if (!_referenceBufferReady || !_externalBufferReady)
        {
            return;
        }

        // if both buffers are ready, resample them
        var result =    ResampleBuffer(_poseBufferReference, out _poseListReferenceNormalized) &&
                        ResampleBuffer(_poseBufferExternal, out _poseListExternalNormalized);

        // clear buffers (should be empty anyway)
        _referenceBufferReady = false;
        _externalBufferReady = false;
        _poseBufferReference = new RingBuffer<TimedPosition>(_bufferSize);
        _poseBufferExternal = new RingBuffer<TimedPosition>(_bufferSize);

        // abort if resampling failed
        if (!result)
        {
            return;
        }

        FastGlobalRegistration();

        // ALIGNMENT
        //// produce aligned list of poses
        //var alignedTupleList = GenerateAlignedSequence();

        //// check list, abort if failed
        //if (alignedTupleList == null || alignedTupleList.Count < 3)
        //{
        //    return;
        //}

        // TODO: compute registration using aligned tuple list
        // ...
    }

    private void FastGlobalRegistration()
    {
        List<Vector3> refPoints = new List<Vector3>(_poseListReferenceNormalized.Count);
        foreach(var pt in _poseListReferenceNormalized)
        {
            refPoints.Add(pt.Position);
        }

        List<Vector3> extPoints = new List<Vector3>(_poseListExternalNormalized.Count);
        foreach (var pt in _poseListExternalNormalized)
        {
            extPoints.Add(pt.Position);
        }

        fgr.CApp app = new fgr.CApp();
        app.AddFeature(refPoints);
        app.AddFeature(extPoints);
        app.NormalizePoints();
        app.AdvancedMatching();
        app.OptimizePairwise(true);
        var mat = app.GetOutputTrans();
        Debug.Log("Output: \n" + mat);
        app.WriteTrans("Assets/cloudoutput.txt");
    }

    private bool ResampleBuffer(RingBuffer<TimedPosition> buffer, out List<TimedPosition> output)
    {
        // initialize output list
        output = new List<TimedPosition>((int)(_samplingRate * _windowSize));

        // check buffer size
        if (buffer.Count < 2)
        {            
            return false;
        }

        // get first and last element
        var success = buffer.TryPeek(0, out var firstPose) & buffer.TryPeek(buffer.Count - 1, out var lastPose);
        if (!success)
        {
            return false;
        }

        // compute duration of sequence, check if long enough for the window size
        var duration = (lastPose.Time - firstPose.Time).TotalSeconds;
        if (duration < _windowSize)
        {
            return false;
        }

        // write first element
        var currentElement = buffer.Get(); // gets removed from buffer
        var currentTime = firstPose.Time;
        output.Add(currentElement);

        // try to get second element
        if (!buffer.TryPeek(0, out var nextElement))
        {
            output.Clear();
            return false;
        }

        // re-sample the rest of the data
        for (int i = 1; i < output.Capacity; i++)
        {
            // update time for next sample
            currentTime += TimeSpan.FromSeconds(i * (1f / _samplingRate));

            // get the two elements surrounding the current time
            while (currentTime > nextElement.Time && buffer.Count > 0)
            {
                currentElement = nextElement; 
                nextElement = buffer.Get();
            }

            // interpolate between elements based on current time, add to list
            output.Add(InterpolatePosition(currentElement, nextElement, currentTime));
        }

        return true;
    }

    private TimedPosition InterpolatePosition(TimedPosition firstPosition, TimedPosition secondPosition, DateTimeOffset time)
    {
        Vector3 interpolatedPosition = Vector3.Lerp(firstPosition.Position, secondPosition.Position, (float)(time - firstPosition.Time).Ticks / (secondPosition.Time - firstPosition.Time).Ticks);
        return new TimedPosition(interpolatedPosition, time);
    }

    private List<Tuple<TimedPosition, TimedPosition>> GenerateAlignedSequence()
    {
        int maxOffset = (int)(_maxDelay * _samplingRate);

        // compute velocity data
        List<float> vReference = new List<float>(_poseListReferenceNormalized.Count - 1);
        for (int i = 0; i < vReference.Capacity; i++)
        {
            vReference.Add((_poseListReferenceNormalized[i + 1].Position - _poseListReferenceNormalized[i].Position).magnitude);
        }

        List<float> vExternal = new List<float>(_poseListExternalNormalized.Count - 1);
        for (int i = 0; i < vExternal.Capacity; i++)
        {
            vExternal.Add((_poseListExternalNormalized[i + 1].Position - _poseListExternalNormalized[i].Position).magnitude);
        }

        float bestError = float.MaxValue;
        int bestAlignment = 0;

        int n1 = 0, n2 = 0;

        // try to align left
        for (int i = 0; i < maxOffset; i++)
        {
            float error = 0f;
            n1 = 0; n2 = n1 + i;
            int count = 0;
            while (n1 < vReference.Count && n2 < vExternal.Count)
            {
                error += (vReference[n1] - vExternal[n2]) * (vReference[n1] - vExternal[n2]);
                count++;
                n1++; n2++;
            }

            error /= count;

            if (error < bestError)
            {
                bestError = error;
                bestAlignment = i;
            }
        }

        // try to align right
        for (int i = 0; i > -maxOffset; i--)
        {
            float error = 0f;
            n2 = 0; n1 = n2 - i;
            int count = 0;
            while (n1 < vReference.Count && n2 < vExternal.Count)
            {
                error += (vReference[n1] - vExternal[n2]) * (vReference[n1] - vExternal[n2]);
                count++;
                n1++; n2++;
            }

            error /= count;

            if (error < bestError)
            {
                bestError = error;
                bestAlignment = i;
            }
        }

        // generate tuple list
        List<Tuple<TimedPosition, TimedPosition>> result = new List<Tuple<TimedPosition, TimedPosition>>();
        if (bestAlignment >= 0)
        {
            n1 = 0;
            n2 = n1 + bestAlignment; 
        }
        else
        {
            n2 = 0;
            n1 = n2 - bestAlignment;
        }

        while (n1 < _poseListReferenceNormalized.Count && n2 < _poseListExternalNormalized.Count)
        {
            result.Add(new Tuple<TimedPosition, TimedPosition>(_poseListReferenceNormalized[n1], _poseListExternalNormalized[n2]));
            n1++; n2++;
        }

        return result;
    }

    private DateTimeOffset GetBufferStartTime(RingBuffer<TimedPosition> buffer)
    {
        if (buffer.TryPeek(0, out TimedPosition data))
        {
            return data.Time;
        }
        else
        {
            return DateTimeOffset.UtcNow;
        }
    }

    //private void MatchPoses()
    //{
    //    // 1.   re-sample delayed poses to be in same sample rate
    //    // 1.1. compute sample rate of reference poses
    //    int sampleRateReference = 0;
    //    if (_poseBufferReference.TryPeek(0, out var firstSample) && _poseBufferReference.TryPeek(_poseBufferReference.Count-1, out var lastSample))
    //    {
    //        sampleRateReference = (int)((float)(lastSample.Time.Ticks - firstSample.Time.Ticks) / 10000000) / _poseBufferReference.Count;
    //    }
    //}

    private void Start()
    {
        _poseBufferReference = new RingBuffer<TimedPosition>(_bufferSize);
        _poseBufferExternal = new RingBuffer<TimedPosition>(_bufferSize);
        _poseListReferenceNormalized = new List<TimedPosition>((int)(_samplingRate * _windowSize));
        _poseListExternalNormalized = new List<TimedPosition>((int)(_samplingRate * _windowSize));
    }

    private class TimedPosition
    {
        public Vector3 Position;
        public DateTimeOffset Time;
        public TimedPosition(Vector3 position, DateTimeOffset time)
        {
            Position = position;
            Time = time;
        }
    }

}
