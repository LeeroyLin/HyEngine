using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Log;

namespace Engine.Scripts.Runtime.Net
{
    public abstract class SocketConnectionBase : ISocketConnection
    {
        static readonly int HEADER_LEN = 6;
        
        public string Host { get; private set; }
        public int Port { get; private set; }
        public Socket Socket { get; private set; }
        
        public Action<Socket> OnConnected { get; set; }
        public Action OnDisconnected { get; set; }
        public Action<int, int, byte[]> OnRecData { get; set; }
        
        byte[] _bytes = new byte[1024];
        byte[] _byte2 = new byte[2];
        byte[] _byte4 = new byte[4];
        
        MemoryStream _stream = new MemoryStream();

        protected LogGroup log;
        
        private int _serialId = 0;
        private int _protoId = 0;
        private int _contentLen = 0;
        private bool _isHasData = false;

        public SocketConnectionBase(string host, int port)
        {
            Host = host;
            Port = port;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            log = new LogGroup($"Conn {host}:{port}");
        }

        public void Connect()
        {
            log.Log($"Start connect to {Host}:{Port}");
            
            try
            {
                Socket.Connect(Host, Port);
            }
            catch (Exception e)
            {
                log.Error($"Connect to {Host}:{Port} failed. {e.Message}");
                
                return;
            }

            log.Log($"Connect to {Host}:{Port} success.");

            OnConnected?.Invoke(Socket);

            StartReceive();
        }

        public void Disconnect()
        {
            try
            {
                Socket.Disconnect(false);
            }
            catch (Exception e)
            {
                log.Error($"Disconnect to {Host}:{Port} failed. {e.Message}");
                
                return;
            }

            log.Log($"Disconnect to {Host}:{Port} success.");
            
            OnDisconnected?.Invoke();
        }

        public void Shutdown()
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
        }

        public void SendMsg(byte[] bytes)
        {
            try
            {
                Socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
            }
            catch (Exception e)
            {
                log.Error($"Send message failed. {e.Message}");
            }
        }

        /// <summary>
        /// 开始接收数据
        /// 包头：协议号2b 序列号2b 内容长度2b
        /// </summary>
        async void StartReceive()
        {
            log.Log($"Start receive server msg data...");
            
            ResetStream();

            while (Socket.Connected)
            {
                if (!_isHasData)
                    ReadMsgHeader();

                if (!_isHasData)
                    return;
            
                ReadMsgContent();
                
                await Task.Delay(1);
            }
            
            log.Log($"Finish receive server msg data.");
        }

        void ResetStream()
        {
            _stream.Position = 0;
            _stream.SetLength(0);
        }

        void ReadMsgHeader()
        {
            if (_stream.Length < HEADER_LEN)
            {
                int length = Socket.Receive(_bytes);

                if (length == 0 && _stream.Length == 0)
                    return;
                
                if (length > 0)
                    _stream.Write(_bytes, 0, length);
            
                if (_stream.Length < HEADER_LEN)
                {
                    log.Error($"Msg head length '{_stream.Length}' not enough.");
                
                    ResetStream();
                
                    return;
                }
            }
            
            _serialId = ReadByte2FromStream();
            _protoId = ReadByte2FromStream();
            _contentLen = ReadByte4FromStream();
            
            _isHasData = true;
        }

        void ReadMsgContent()
        {
            if (_stream.Length < _contentLen)
            {
                int length = Socket.Receive(_bytes);

                if (length > 0)
                    _stream.Write(_bytes, 0, length);
                else
                {
                    log.Error($"Msg content length '{_stream.Length}' not enough. Head set length is: '{_contentLen}'");

                    ResetStream();
                    
                    _isHasData = false;
                    
                    return;
                }

                if (_stream.Length < _contentLen)
                    return;
            }
            
            var contentBytes = new byte[_contentLen];
            _stream.Read(contentBytes, 0, _contentLen);

            _isHasData = false;
            
            // 回调
            OnRecData?.Invoke(_protoId, _serialId, contentBytes);
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