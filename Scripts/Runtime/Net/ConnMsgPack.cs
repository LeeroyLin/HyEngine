using System;
using System.IO;
using Engine.Scripts.Runtime.Encrypt;
using UnityEngine;

namespace Engine.Scripts.Runtime.Net
{
    public class ConnMsgPack : IConnMsgPack
    {
        protected byte[] _byte2 = new byte[2];
        protected byte[] _byte2_2 = new byte[2];
        protected byte[] _byte4 = new byte[4];
        protected byte[] _byte8 = new byte[8];
        protected NetMsg NetMsg;

        protected UInt32 contentLen;

        protected MemoryStream packStream;

        public ConnMsgPack()
        {
            packStream = new MemoryStream();
            RC4.SetKey("Mz,*'cP,.:'|z2");
        }
        
        public int HeadLen()
        {
            return 8;
        }

        public int ContentLen()
        {
            return (int)NetMsg.ContentLen;
        }

        public byte[] Pack(NetMsg msg, bool isEncrypt)
        {
            packStream.Position = 0;
            packStream.SetLength(0);

            _byte2 = BitConverter.GetBytes(msg.MsgId);
            _byte2_2 = BitConverter.GetBytes(msg.ProtoId);
            _byte4 = BitConverter.GetBytes(msg.ContentLen);
            
            if (isEncrypt)
            {
                Array.Copy(_byte2, 0, _byte8, 0, 2);
                Array.Copy(_byte2_2, 0, _byte8, 2, 2);
                Array.Copy(_byte4, 0, _byte8, 4, 4);
                _byte8 = RC4.Encrypt(_byte8);
                packStream.Write(_byte8);
            }
            else
            {
                packStream.Write(_byte2);
                packStream.Write(_byte2_2);
                packStream.Write(_byte4);
            }
            
            packStream.Write(msg.Data);

            int len = HeadLen() + (int)msg.ContentLen;
            byte[] bytes = new byte[len];

            packStream.Position = 0;
            packStream.Read(bytes, 0, len);
            
            return bytes;
        }

        public void UnPackHead(MemoryStream stream, bool isEncrypt)
        {
            ushort serialId = 0;
            ushort protoId = 0;
            
            stream.Position = 0;

            if (isEncrypt)
            {
                stream.Read(_byte8, 0, 8);
                _byte8 = RC4.Decrypt(_byte8);
                
                serialId = GetIntFromByteArrWith2Bit(_byte8, 0);
                protoId = GetIntFromByteArrWith2Bit(_byte8, 2);
                contentLen = GetIntFromByteArrWith4Bit(_byte8, 4);
            }
            else
            {
                serialId = ReadByte2FromStream(stream);
                protoId = ReadByte2FromStream(stream);
                contentLen = ReadByte4FromStream(stream);
            }
            
            NetMsg = new NetMsg(serialId, protoId, contentLen);
            
            stream.Position = stream.Length - 1;
        }

        public NetMsg UnPackContent(MemoryStream stream, bool isEncrypt)
        {
            stream.Position = 0;
            
            var contentBytes = new byte[ContentLen()];
            stream.Read(contentBytes, 0, ContentLen());
            
            NetMsg.SetData(contentBytes);
            
            stream.Position = stream.Length - 1;

            return NetMsg;
        }
        
        protected ushort ReadByte2FromStream(MemoryStream stream)
        {
            stream.Read(_byte2, 0, 2);

            return BitConverter.ToUInt16(_byte2);
        }

        protected UInt32 ReadByte4FromStream(MemoryStream stream)
        {
            stream.Read(_byte4, 0, 4);

            return BitConverter.ToUInt32(_byte2);
        }

        protected ushort GetIntFromByteArrWith2Bit(byte[] bytes, int offset)
        {
            Array.Copy(bytes, 0, _byte2, offset, 2);
            return BitConverter.ToUInt16(_byte2);
        }

        protected UInt32 GetIntFromByteArrWith4Bit(byte[] bytes, int offset)
        {
            Array.Copy(bytes, 0, _byte4, offset, 4);
            return BitConverter.ToUInt32(_byte4);
        }
    }
}