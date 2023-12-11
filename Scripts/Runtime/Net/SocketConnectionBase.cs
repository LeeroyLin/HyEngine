using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Engine.Scripts.Runtime.Net
{
    public class SocketConnectionBase : ISocketConnection
    {
        static readonly int HEADER_LEN = 8;
        
        public string Host { get; private set; }
        public int Port { get; private set; }
        public Socket Socket { get; private set; }
        
        public Action<Socket> OnConnected { get; set; }
        public Action OnDisconnected { get; set; }
        public Action<int, int, byte[]> OnRecData { get; set; }
        
        byte[] _bytes = new byte[1024 * 3];
        byte[] _byte2 = new byte[2];
        byte[] _byte4 = new byte[4];
        
        MemoryStream _stream = new MemoryStream();

        public SocketConnectionBase(string host, int port)
        {
            Host = host;
            Port = port;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connect()
        {
            Socket.BeginConnect(Host, Port, OnSocketConnected, null);
        }

        public void Disconnect()
        {
            Socket.BeginDisconnect(true, OnSocketDisconnected, null);
        }

        public void Shutdown()
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
        }

        public void SendMsg(byte[] bytes)
        {
            // 包头拼接
            
            Socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, OnSocketSend, null);
        }

        void OnSocketConnected(IAsyncResult ar)
        {
            Socket.EndConnect(ar);
            OnConnected?.Invoke(Socket);

            StartReceive();
        }

        void OnSocketDisconnected(IAsyncResult ar)
        {
            Socket.EndDisconnect(ar);
            OnDisconnected?.Invoke();
        }

        void OnSocketSend(IAsyncResult ar)
        {
            Socket.EndSend(ar);
        }

        /// <summary>
        /// 开始接收数据
        /// 包头：协议号2b 序列号2b 内容长度4b
        /// </summary>
        async void StartReceive()
        {
            while (Socket.Connected)
            {
                ReadData();
                
                await Task.Delay(20);
            }
        }

        void ReadData()
        {
            _stream.Position = 0;
            _stream.SetLength(0);
            
            int length = Socket.Receive(_bytes);
    
            if (length == 0)
                return;
            
            _stream.Write(_bytes, 0, length);

            while (_stream.Length > 0)
            {
                if (_stream.Length <= HEADER_LEN)
                {
                    // todo 数据过短
                
                    return;
                }
                
                ReadOneProtoData();
            }
        }

        void ReadOneProtoData()
        {
            int protoId = ReadByte2FromStream();
            int serialId = ReadByte2FromStream();
            int contentLen = ReadByte4FromStream();

            while (_stream.Length < contentLen)
            {
                int length = Socket.Receive(_bytes);

                if (length == 0)
                {
                    // todo 内容数据不完整
                    return;
                }
                
                _stream.Write(_bytes, 0, length);
            }
            
            var contentBytes = new byte[contentLen];
            _stream.Read(contentBytes, 0, contentLen);
            
            // 回调
            OnRecData?.Invoke(protoId, serialId, contentBytes);
        }

        protected int ReadByte2FromStream()
        {
            _stream.Read(_byte2, 0, 2);

            return BitConverter.ToInt32(_byte2);
        }

        protected int ReadByte4FromStream()
        {
            _stream.Read(_byte4, 0, 4);

            return BitConverter.ToInt32(_byte2);
        }
    }
}