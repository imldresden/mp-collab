namespace IMLD.MixedReality.Network
{
    public class MessageGenericByteData
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.BYTE_ARRAY;

        public byte[] Data;

        public MessageGenericByteData(byte[] anchorData)
        {
            Data = anchorData;
        }

        public MessageContainer Pack()
        {
            // create message buffer, pre-filled with header
            MessageContainer.CreateBuffer(Data.Length, Type, out byte[] Buffer, out int Offset);

            // copy actual data to buffer
            System.Buffer.BlockCopy(Data, 0, Buffer, Offset, Data.Length);
            return new MessageContainer(Type, Buffer);
        }

        public static MessageGenericByteData Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            return new MessageGenericByteData(container.Payload);
        }
    }
}