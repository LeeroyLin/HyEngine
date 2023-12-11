namespace Engine.Scripts.Runtime.Net
{
    public interface ISocketConnection
    {
        void Connect();
        void Disconnect();
        void Shutdown();
        void SendMsg(byte[] bytes);
    }
}