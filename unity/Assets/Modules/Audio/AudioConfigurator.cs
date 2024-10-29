using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using UnityEngine;

namespace IMLD.MixedReality.Audio
{   
    public enum AudioPlacement
    {
        Spatial, 
        Fixed,
        Stereo        
    }       

    public class AudioConfigurator : MonoBehaviour
    {
        public GameObject fixedAudioPosition;
        public GameObject sceneOrigin;
        private AudioPlacement currentPlacement;
        public GameObject audioObject;
        public GameObject localUser;        
        public int audio = 0;
        public bool global = false;

        private ISessionManager _clientAppStateManager;

        // Start is called before the first frame update
        void Start()
        {
            _clientAppStateManager = ServiceLocator.Instance.Get<ISessionManager>();
            currentPlacement = AudioPlacement.Spatial; 
        }

        // Update is called once per frame
        void Update()
        {
            if (audio == 0)
            {
                changeAudioPlacement(0);
            } else if(audio == 1)
            {
                changeAudioPlacement(1);
            } else if(audio == 2) {
                changeAudioPlacement(2);
            }

        }     

        public void changeAudioPlacement(int audioID)
        {               
            AudioPlacement placement = new AudioPlacement();
            audio = audioID;

            switch (audio)
            {
                case 0:
                    placement = AudioPlacement.Spatial;
                    break;
                case 1:
                    placement = AudioPlacement.Fixed;
                    break;
                case 2:
                    placement = AudioPlacement.Stereo;  
                    break; 
                default:
                break;
            }
            User[] users = sceneOrigin.GetComponentsInChildren<User>(true);

            foreach (User user in users) {
                adaptAudioScene(placement,user);                
            }
            currentPlacement = placement;

            // Send overNetwork
            if (global)
            {
                _clientAppStateManager.UpdateAudioPosition(audioID);
            }
        }

        public void setAudioPlacementGlobally()
        {
            _clientAppStateManager.UpdateAudioPosition(audio);
        }

        void adaptAudioScene(AudioPlacement newPlacement, User user)
        {
            if (newPlacement != currentPlacement) 
            {
                if (newPlacement == AudioPlacement.Fixed || newPlacement == AudioPlacement.Stereo)
                {
                    if (currentPlacement == AudioPlacement.Stereo)
                    {
                        DestroyAudioPlayers(localUser);
                    }
                    else if (currentPlacement == AudioPlacement.Fixed)
                    {
                        DestroyAudioPlayers(sceneOrigin);
                    }

                    GameObject audio = Instantiate(audioObject);
                    // Position? 
                    switch (newPlacement)
                    {
                        case AudioPlacement.Fixed:
                            audio.transform.position = fixedAudioPosition.transform.position;
                            audio.transform.parent = sceneOrigin.transform;
                            break;
                        case AudioPlacement.Stereo:
                            audio.transform.position = localUser.transform.position;
                            audio.transform.parent = localUser.transform;
                            break;
                        default:
                            break;
                    }

                    audio.GetComponent<AudioPlayer>().setAudioSource(user.GetComponent<AudioSource>());
                    user.AudioPlayer = audio.GetComponent<AudioPlayer>();

                    if (currentPlacement == AudioPlacement.Spatial)
                    {                        
                        user.GetComponent<AudioPlayer>().enabled = false;
                        user.GetComponent<AudioSource>().enabled = false;
                    } 
                    
                   
                }
                else if(newPlacement == AudioPlacement.Spatial)
                {
                    if (currentPlacement == AudioPlacement.Fixed)
                    {
                        // if currentplacement ist Fixed
                        // get all Gameobjects in the scene that have an audioplayer but no user script 
                        DestroyAudioPlayers(sceneOrigin);
                    }
                    else if (currentPlacement == AudioPlacement.Stereo)
                    {
                        DestroyAudioPlayers(localUser);
                    }
                    // reactivate the audiosource on all user objects 
                    user.GetComponent<AudioPlayer>().enabled = true;  
                    user.GetComponent<AudioSource>().enabled = true;
                    // user.gameObject.GetComponent<Au>().                        
                    user.AudioPlayer = user.gameObject.GetComponent<AudioPlayer>();
                    user.GetComponent<AudioPlayer>().setAudioSource(user.GetComponent<AudioSource>());                 
                                       
                }                
            }
        }

        private void DestroyAudioPlayers(GameObject parent)
        {
            AudioPlayer[] players = parent.GetComponentsInChildren<AudioPlayer>(); 
            // Debug.Log("Destroying ... " + players.Length);

            if (players != null)
            {  
                foreach (AudioPlayer player in players)
                {
                    if (player.gameObject.GetComponent<User>() == null)
                    {
                        Destroy(player.gameObject);
                    }
                }
            }

        }
    }
}

