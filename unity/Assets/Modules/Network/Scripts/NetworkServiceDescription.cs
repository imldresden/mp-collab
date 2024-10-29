using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class NetworkServiceDescription : IEquatable<NetworkServiceDescription>
    {
        public enum ServiceType
        {
            UNDEFINED,
            APP_STATE,
            AUDIO,
            KINECT_DATA,
            TEST
        }

        public Guid ServiceId { get; set; }
        public Guid SessionId { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public string HostName { get; set; }
        public ServiceType Type { get; set; }
        public string Description { get { return HostName + " (" + IP + "): " + Enum.GetName(typeof(ServiceType), Type); } }
        public string Data { get; set; }
        public int RoomId { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            NetworkServiceDescription objNetworkService = obj as NetworkServiceDescription;
            if (objNetworkService == null) return false;
            else return Equals(objNetworkService);
        }

        public override int GetHashCode()
        {
            int hash = IP.GetHashCode() * 251;
            hash += Port;
            hash *= 251;
            hash += HostName.GetHashCode();
            hash *= 251;
            hash += (int)Type;
            hash *= 251;
            hash += Description.GetHashCode();
            hash *= 251;
            hash += RoomId;
            hash *= 251;
            hash += ServiceId.GetHashCode();
            hash *= 251;
            return hash;
        }

        public bool Equals(NetworkServiceDescription other)
        {
            if (other == null) return false;
            //return IP == other.IP && Port == other.Port && HostName == other.HostName && Type == other.Type && Description == other.Description && RoomId == other.RoomId && ServiceId == other.ServiceId;
            //return IP == other.IP && Port == other.Port;
            return ServiceId == other.ServiceId;
        }
        public static bool operator ==(NetworkServiceDescription desc1, NetworkServiceDescription desc2)
        {
            if (((object)desc1) == null || ((object)desc2) == null)
                return System.Object.Equals(desc1, desc2);

            return desc1.Equals(desc2);
        }

        public static bool operator !=(NetworkServiceDescription desc1, NetworkServiceDescription desc2)
        {
            if (((object)desc1) == null || ((object)desc2) == null)
                return ! System.Object.Equals(desc1, desc2);

            return ! desc1.Equals(desc2);
        }
    }
}