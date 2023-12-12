using System;
using Engine.Scripts.Runtime.Encrypt;

namespace Engine.Scripts.Runtime.Net
{
    public class ConnMsgPack : IConnMsgPack
    {
        protected byte[] _byte2 = new byte[2];
        protected byte[] _byte2_2 = new byte[2];
        protected byte[] _byte4 = new byte[4];
        protected byte[] _byte8 = new byte[8];

        protected NetMsg netMsg;
        protected UInt32 contentLen;

        protected QueueBuffer buffer;

        public ConnMsgPack()
        {
            buffer = new QueueBuffer();
            RC4.SetKey("Mz,*'cP,.:'|z2");
        }

        public int HeadLen()
        {
            return 8;
        }

        public int ContentLen()
        {
            return (int)netMsg.ContentLen;
        }

        public byte[] Pack(NetMsg msg, bool isEncrypt)
        {
            buffer.Reset();

            _byte2 = BitConverter.GetBytes(msg.MsgId);
            _byte2_2 = BitConverter.GetBytes(msg.ProtoId);
            _byte4 = BitConverter.GetBytes(msg.ContentLen);
            
            if (isEncrypt)
            {
                Array.Copy(_byte2, 0, _byte8, 0, 2);
                Array.Copy(_byte2_2, 0, _byte8, 2, 2);
                Array.Copy(_byte4, 0, _byte8, 4, 4);
                _byte8 = RC4.Encrypt(_byte8);
                buffer.Write(_byte8);
            }
            else
            {
                buffer.Write(_byte2);
                buffer.Write(_byte2_2);
                buffer.Write(_byte4);
            }
            
            buffer.Write(msg.Data);

            int len = HeadLen() + (int)msg.ContentLen;
            byte[] bytes = new byte[len];

            buffer.Read(bytes, len);
            
            return bytes;
        }

        public void UnPackHead(QueueBuffer buffer, bool isEncrypt)
        {
            ushort serialId = 0;
            ushort protoId = 0;
            
            if (isEncrypt)
            {
                buffer.Read(_byte8, 8);
                _byte8 = RC4.Decrypt(_byte8);
                
                serialId = GetIntFromByteArrWith2Bit(_byte8, 0);
                protoId = GetIntFromByteArrWith2Bit(_byte8, 2);
                contentLen = GetIntFromByteArrWith4Bit(_byte8, 4);
            }
            else
            {
                serialId = ReadByte2FromStream(buffer);
                protoId = ReadByte2FromStream(buffer);
                contentLen = ReadByte4FromStream(buffer);
            }
            
            netMsg = new NetMsg(serialId, protoId, contentLen);
        }

        public NetMsg UnPackContent(QueueBuffer buffer, bool isEncrypt)
        {
            var contentBytes = new byte[ContentLen()];
            buffer.Read(contentBytes, ContentLen());
            
            netMsg.SetData(contentBytes);

            return netMsg;
        }
        
        protected ushort ReadByte2FromStream(QueueBuffer buffer)
        {
            buffer.Read(_byte2, 2);

            return BitConverter.ToUInt16(_byte2);
        }

        protected UInt32 ReadByte4FromStream(QueueBuffer buffer)
        {
            buffer.Read(_byte4, 4);

            return BitConverter.ToUInt32(_byte2);
        }

        protected ushort GetIntFromByteArrWith2Bit(byte[] bytes, int offset)
        {
            Array.Copy(bytes, offset, _byte2, 0, 2);
            return BitConverter.ToUInt16(_byte2);
        }

        protected UInt32 GetIntFromByteArrWith4Bit(byte[] bytes, int offset)
        {
            Array.Copy(bytes, offset, _byte4, 0, 4);
            return BitConverter.ToUInt32(_byte4);
        }
    }
}