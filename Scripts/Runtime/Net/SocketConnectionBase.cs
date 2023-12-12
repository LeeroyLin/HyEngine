using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Log;

namespace Engine.Scripts.Runtime.Net
{
    public abstract class SocketConnectionBase : ISocketConnection
    {
        public string Host { get; private set; }
        public int Port { get; private set; }
        public Socket Socket { get; private set; }
        public bool IsEncrypt { get; private set; }
        
        public Action<Socket> OnConnected { get; set; }
        public Action OnDisconnected { get; set; }
        public Action<NetMsg> OnRecData { get; set; }
        public IConnMsgPack MsgPack { get; protected set; }
        public LogGroup Log { get; protected set; }
        
        public int MaxMsgContentLen { get; protected set; }
        public ushort MsgId { get; private set; }

        byte[] _bytes = new byte[1024];
        
        MemoryStream _stream = new MemoryStream();
        
        private bool _isHasData = false;

        public SocketConnectionBase(string host, int port, IConnMsgPack msgPack, bool isEncrypt, int maxMsgContentLen)
        {
            Log = new LogGroup($"Conn {host}:{port}");
            
            Host = host;
            Port = port;
            MsgPack = msgPack;
            IsEncrypt = isEncrypt;
            MaxMsgContentLen = maxMsgContentLen;
            
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            MsgId = 0;
        }

        public void Connect()
        {
            Log.Log($"Start connect to {Host}:{Port}");
            
            try
            {
                Socket.Connect(Host, Port);
            }
            catch (Exception e)
            {
                Log.Error($"Connect to {Host}:{Port} failed. {e.Message}");
                
                return;
            }

            Log.Log($"Connect to {Host}:{Port} success.");

            StartReceive();

            OnConnected?.Invoke(Socket);
        }

        public void Disconnect()
        {
            try
            {
                Socket.Disconnect(false);
            }
            catch (Exception e)
            {
                Log.Error($"Disconnect to {Host}:{Port} failed. {e.Message}");
                
                return;
            }

            Log.Log($"Disconnect to {Host}:{Port} success.");
            
            OnDisconnected?.Invoke();
        }

        public void Shutdown()
        {
            if (Socket.Connected)
            {
                Socket.Shutdown(SocketShutdown.Both);   

                Log.Log($"Shutdown connection {Host}:{Port} success.");
            }
            
            Socket.Close();
        }

        public void SendMsg(ushort protoId, byte[] bytes)
        {
            if (bytes.Length > MaxMsgContentLen)
            {
                Log.Error($"Send msg failed. Msg len '{bytes.Length}' lager than max msg len '{MaxMsgContentLen}'.");
                
                return;
            }
            
            var msg = new NetMsg(MsgId, protoId, (UInt32)bytes.Length, bytes);

            bytes = MsgPack.Pack(msg, IsEncrypt);
            
            try
            {
                Socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
            }
            catch (Exception e)
            {
                Log.Error($"Send message failed. {e.Message}");
            }

            AddMsgId();
        }

        protected void AddMsgId()
        {
            if (MsgId == ushort.MaxValue)
                MsgId = 0;
            else            
                MsgId++;
        }

        /// <summary>
        /// 开始接收数据
        /// 包头：协议号2b 序列号2b 内容长度2b
        /// </summary>
        async void StartReceive()
        {
            Log.Log($"Start receive server msg data...");
            
            ResetStream();

            while (Socket.Connected)
            {
                await Task.Delay(1);

                var isContinue = true;
                
                if (!_isHasData)
                {
                    isContinue = await ReadMsgHeader();
                    if (!isContinue)
                        break;
                }

                if (!_isHasData)
                    break;

                isContinue = await ReadMsgContent();
                if (!isContinue)
                    break;
            }
            
            Log.Log($"Finish receive server msg data.");
        }

        void ResetStream()
        {
            _stream.Position = 0;
            _stream.SetLength(0);
        }

        async Task<bool> ReadMsgHeader()
        {
            if (_stream.Length < MsgPack.HeadLen())
            {
                int length = 0;

                try
                {
                    length = await Socket.ReceiveAsync(_bytes, SocketFlags.None);
                }
                catch (Exception e)
                {
                    Log.Error($"Receive msg error: '{e.Message}'.");
                    
                    Shutdown();
                    
                    return false;
                }

                if (length == 0 && _stream.Length == 0)
                    return true;
                
                if (length > 0)
                    _stream.Write(_bytes, 0, length);
            
                if (_stream.Length < MsgPack.HeadLen())
                {
                    Log.Error($"Msg head length '{_stream.Length}' not enough.");
                
                    ResetStream();

                    return true;
                }
            }

            MsgPack.UnPackHead(_stream, IsEncrypt);

            int contentLen = MsgPack.ContentLen();
            if (contentLen > MaxMsgContentLen)
            {
                ResetStream();
                
                Log.Error($"Receive msg len '{contentLen}' lager than max msg len '{MaxMsgContentLen}'.");
                
                Shutdown();
                
                return false;
            }
            
            _isHasData = true;

            return true;
        }

        async Task<bool> ReadMsgContent()
        {
            int contentLen = MsgPack.ContentLen();
            
            if (_stream.Length < contentLen)
            {
                int length = 0;

                try
                {
                    length = await Socket.ReceiveAsync(_bytes, SocketFlags.None);
                }
                catch (Exception e)
                {
                    Log.Error($"Receive msg error: '{e.Message}'.");
                    
                    Shutdown();
                    
                    return false;
                }
                
                if (length > 0)
                    _stream.Write(_bytes, 0, length);
                else
                {
                    Log.Error($"Msg content length '{_stream.Length}' not enough. Head set length is: '{contentLen}'");

                    ResetStream();
                    
                    _isHasData = false;
                    
                    return true;
                }

                if (_stream.Length < contentLen)
                    return true;
            }

            _isHasData = false;

            var msg = MsgPack.UnPackContent(_stream, IsEncrypt);
            
            // 回调
            OnRecData?.Invoke(msg);

            return true;
        }
    }
}