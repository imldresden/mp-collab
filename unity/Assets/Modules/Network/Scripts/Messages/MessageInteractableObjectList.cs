using IMLD.MixedReality.Core;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace IMLD.MixedReality.Network
{
    public class MessageInteractableObjectList : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.OBJECT_LIST;

        public Dictionary<int, InteractableObjectStruct> Interactables;

        public MessageInteractableObjectList()
        {
            // Empty constructor for serialization
        }

        public MessageInteractableObjectList(List<IInteractableObject> objects)
        {
            Interactables = new Dictionary<int, InteractableObjectStruct>(objects.Count);
            foreach( var obj in objects)
            {
                Interactables.Add(obj.Id, new InteractableObjectStruct
                {
                    Position = Conversion.FromUnityVector3(obj.GetPose().position),
                    Rotation = Conversion.FromUnityQuaternion(obj.GetPose().rotation)
                });
            }
        }

        public MessageInteractableObjectList(Dictionary<int, InteractableObjectStruct> interactables)
        {
            Interactables = interactables;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageInteractableObjectList Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            try
            {
                var Result = JsonConvert.DeserializeObject<MessageInteractableObjectList>(Encoding.UTF8.GetString(container.Payload));
                return Result;
            }
            catch (Exception e)
            {
                Debug.LogError("Error unpacking message: " + e.Message);
                return null;
            }

        }

        public struct InteractableObjectStruct
        {
            public System.Numerics.Vector3 Position;
            public System.Numerics.Quaternion Rotation;
        }
    }
}
