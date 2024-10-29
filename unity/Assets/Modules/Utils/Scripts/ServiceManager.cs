using IMLD.MixedReality.Audio;
using IMLD.MixedReality.Avatars;
using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Utils
{
    /// <summary>
    /// This component serves as a simple user-facing interface for the the ServiceLocator class.
    /// </summary>
    public class ServiceManager : MonoBehaviour
    {
        public NetworkServiceManager NetworkServiceManager;
        public KinectManager KinectManager;
        public BaseAppStateManager AppStateManager;
        public WorldAnchor WorldAnchor;
        public StudyManager StudyManager;
        public InteractableManager InteractableManager;
        public AudioReceiver AudioReceiver;
        public AbstractLog Log;
        public PlaybackControl PlaybackControl;
        public Config Config;

        void Awake()
        {
            if (NetworkServiceManager != null && NetworkServiceManager is INetworkServiceManager)
            {
                ServiceLocator.Instance.Register(typeof(INetworkServiceManager), NetworkServiceManager);
            }

            if (KinectManager != null && KinectManager is IKinectManager)
            {
                ServiceLocator.Instance.Register(typeof(IKinectManager), KinectManager);
            }


            if (AppStateManager != null && AppStateManager is ISessionManager)
            {
                ServiceLocator.Instance.Register(typeof(ISessionManager), AppStateManager);
            }

            if (WorldAnchor != null && WorldAnchor is IWorldAnchor)
            {
                ServiceLocator.Instance.Register(typeof(IWorldAnchor), WorldAnchor);
            }

            if (StudyManager != null && StudyManager is IStudyManager)
            {
                ServiceLocator.Instance.Register(typeof(IStudyManager), StudyManager);
            }

            if (InteractableManager != null && InteractableManager is IInteractableManager)
            {
                ServiceLocator.Instance.Register(typeof(IInteractableManager), InteractableManager);
            }

            if (AudioReceiver != null && AudioReceiver is IAudioReceiver)
            {
                ServiceLocator.Instance.Register(typeof(IAudioReceiver), AudioReceiver);
            }

            if (Log != null && Log is ILog)
            {
                ServiceLocator.Instance.Register(typeof(ILog), Log);
            }

            if (PlaybackControl != null)
            {
                ServiceLocator.Instance.Register(typeof(PlaybackControl), PlaybackControl);
            }

            if (Config != null)
            {
                ServiceLocator.Instance.Register(typeof(Config), Config);
            }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

    }
}
