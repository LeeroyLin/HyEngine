namespace Engine.Scripts.Runtime.Net
{
    public interface ISocketConnection
    {
        void Connect();
        void Disconnect();
        void Shutdown();
        void SendMsg(ushort protoId, byte[] bytes);
    }
}