using IMLD.MixedReality.Core;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace IMLD.MixedReality.Network
{
    public class MessageInteractableObjectUpdate : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.OBJECT_UPDATE;

        public int ID;
        public float posX, posY, posZ, rotX, rotY, rotZ, rotW;

        public MessageInteractableObjectUpdate()
        {

        }

        public MessageInteractableObjectUpdate(int id, Pose pose) : this(id, pose.position, pose.rotation) { }
        public MessageInteractableObjectUpdate(int id, Vector3 position, Quaternion rotation)
        {
            ID = id;

            posX = position.x;
            posY = position.y;
            posZ = position.z;

            rotX = rotation.x;    
            rotY = rotation.y;    
            rotZ = rotation.z;    
            rotW = rotation.w;    
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageInteractableObjectUpdate Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            try
            {
                var Result = JsonConvert.DeserializeObject<MessageInteractableObjectUpdate>(Encoding.UTF8.GetString(container.Payload));
                return Result;
            }
            catch (Exception e)
            {
                Debug.LogError("Error unpacking message: " + e.Message);
                return null;
            }
            
        }
    }
}
