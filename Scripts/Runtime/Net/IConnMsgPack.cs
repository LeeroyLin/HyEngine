namespace Engine.Scripts.Runtime.Net
{
    public interface IConnMsgPack
    {
        int HeadLen();
        int ContentLen();
        byte[] Pack(NetMsg msg, bool isEncrypt);
        void UnPackHead(QueueBuffer buffer, bool isEncrypt);
        NetMsg UnPackContent(QueueBuffer buffer, bool isEncrypt);
    }
}