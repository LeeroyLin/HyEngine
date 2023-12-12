using System;

namespace Engine.Scripts.Runtime.Net
{
    public struct NetMsg
    {
        public ushort MsgId { get; private set; }
        public ushort ProtoId { get; private set; }
        public UInt32 ContentLen { get; private set; }
        public byte[] Data { get; private set; }

        public NetMsg(ushort msgId, ushort protoId, UInt32 contentLen, byte[] data = null)
        {
            MsgId = msgId;
            ProtoId = protoId;
            ContentLen = contentLen;
            Data = data;
        }

        public void SetData(byte[] data)
        {
            Data = data;
        }
    }
}