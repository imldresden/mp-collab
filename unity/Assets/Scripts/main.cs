//using UnityEngine;
//using IMLD.MixedReality.Avatars;

//public class main : MonoBehaviour
//{
//    // Handler for SkeletalTracking thread.
//    public GameObject m_tracker;
//    private KinectTrackingProvider m_skeletalTrackingProvider;
//    public KinectDataFrame m_lastFrameData = new KinectDataFrame();

//    void Start()
//    {
//        //tracker ids needed for when there are two trackers
//        const int TRACKER_ID = 0;
//        m_skeletalTrackingProvider = new KinectTrackingProvider(TRACKER_ID);
//    }

//    void Update()
//    {
        
//        if (m_skeletalTrackingProvider.IsRunning)
//        {
//            if (m_skeletalTrackingProvider.GetCurrentFrameData(ref m_lastFrameData))
//            {
//                if (m_lastFrameData.NumOfBodies != 0)
//                {
//                    m_tracker.GetComponent<TrackerHandler>().updateTracker(m_lastFrameData);
//                }
//            }
//        }
//    }

//    void OnApplicationQuit()
//    {
//        if (m_skeletalTrackingProvider != null)
//        {
//            m_skeletalTrackingProvider.Dispose();
//        }
//    }
//}
