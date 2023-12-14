namespace Engine.Scripts.Runtime.Net
{
    public interface IConnMsgPack
    {
        int HeadLen();
        int ContentLen();
        byte[] Pack(NetMsg msg, bool isEncrypt);
        void UnPackHead(QueueBuffer buf, bool isEncrypt);
        NetMsg UnPackContent(QueueBuffer buf, bool isEncrypt);
    }
}