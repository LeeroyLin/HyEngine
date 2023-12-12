using System.IO;

namespace Engine.Scripts.Runtime.Net
{
    public interface IConnMsgPack
    {
        int HeadLen();
        int ContentLen();
        byte[] Pack(NetMsg msg, bool isEncrypt);
        void UnPackHead(MemoryStream stream, bool isEncrypt);
        NetMsg UnPackContent(MemoryStream stream, bool isEncrypt);
    }
}