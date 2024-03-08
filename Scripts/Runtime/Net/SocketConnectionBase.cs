using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Log;

namespace Engine.Scripts.Runtime.Net
{
    struct MsgQueueData
    {
        public NetMsg NetMsg { get; set; }
        public object UserData { get; set; }
    }
    
    public abstract class SocketConnectionBase : ISocketConnection
    {
        public string Key { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public Socket Socket { get; private set; }
        public bool IsEncrypt { get; private set; }
        
        public Action<string> OnConnected { get; set; }
        public Action<string> OnConnectFailed { get; set; }
        public Action<string> OnDisconnected { get; set; }
        public Action<string> OnShutdown { get; set; }
        public Action<NetMsg, object> OnSendData { get; set; }
        public Action<NetMsg> OnRecData { get; set; }
        public IConnMsgPack MsgPack { get; protected set; }
        public LogGroup Log { get; protected set; }
        
        public int MaxMsgContentLen { get; protected set; }
        public ushort MsgId { get; private set; }

        byte[] _bytes = new byte[1024];
        
        QueueBuffer _buffer = new QueueBuffer();
        
        private bool _isHasData = false;

        private Queue<MsgQueueData> _sendMsgQueue = new Queue<MsgQueueData>();
        private bool _isSending = false;

        public SocketConnectionBase(string host, int port, IConnMsgPack msgPack, bool isEncrypt, int maxMsgContentLen)
        {
            Key = GetKey(host, port);
            
            Log = new LogGroup(Key);
            
            Host = host;
            Port = port;
            MsgPack = msgPack;
            IsEncrypt = isEncrypt;
            MaxMsgContentLen = maxMsgContentLen;
            
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            MsgId = 0;
        }

        public static string GetKey(string host, int port)
        {
            return $"Conn {host}:{port}";
        }

        public async void Connect()
        {
            Log.Log($"Start connect to {Host}:{Port}");
            
            try
            {
                await Socket.ConnectAsync(Host, Port);
            }
            catch (Exception e)
            {
                Log.Error($"Connect to {Host}:{Port} failed. {e.Message}");
                
                OnConnectFailed?.Invoke(Key);
                
                return;
            }

            Log.Log($"Connect to {Host}:{Port} success.");

            StartReceive();

            OnConnected?.Invoke(Key);
        }

        public void Disconnect()
        {
            try
            {
                Socket.Disconnect(true);
            }
            catch (Exception e)
            {
                Log.Error($"Disconnect to {Host}:{Port} failed. {e.Message}");
                
                return;
            }

            Log.Log($"Disconnect to {Host}:{Port} success.");

            _isSending = false;
            
            OnDisconnected?.Invoke(Key);
        }

        public void Shutdown()
        {
            if (Socket == null)
                return;
            
            if (Socket.Connected)
            {
                Socket.Shutdown(SocketShutdown.Both);   

                Log.Log($"Shutdown connection {Host}:{Port} success.");
            }
            
            Socket.Close();
            Socket = null;
            
            _sendMsgQueue.Clear();
            _isSending = false;
            
            OnShutdown?.Invoke(Key);
        }

        public void SendMsg(ushort protoId, byte[] bytes, object userData = null)
        {
            if (bytes.Length > MaxMsgContentLen)
            {
                Log.Error($"Send msg failed. Msg len '{bytes.Length}' lager than max msg len '{MaxMsgContentLen}'.");
                
                return;
            }
            
            AddMsgId();

            var msg = new NetMsg(MsgId, protoId, (UInt32)bytes.Length, bytes);
            
            _sendMsgQueue.Enqueue(new MsgQueueData()
            {
                NetMsg = msg,
                UserData = userData,
            });
            
            TrySendMsg();
        }

        async void TrySendMsg()
        {
            if (_isSending)
                return;
            
            if (_sendMsgQueue.Count == 0)
                return;

            var data = _sendMsgQueue.Dequeue();
            
            var bytes = MsgPack.Pack(data.NetMsg, IsEncrypt);
            
            _isSending = true;
            
            try
            {
                await Socket.SendAsync(bytes, SocketFlags.None);
            }
            catch (Exception e)
            {
                Log.Error($"Send message failed. {e.Message}");
            }
            
            _isSending = false;
            
            OnSendData?.Invoke(data.NetMsg, data.UserData);

            TrySendMsg();
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

            while (Socket != null && Socket.Connected)
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
                    continue;
                
                isContinue = await ReadMsgContent();
                
                if (!isContinue)
                    break;
            }
            
            Log.Log($"Finish receive server msg data.");
        }

        void ResetStream()
        {
            _buffer.Reset();
        }

        async Task<bool> ReadMsgHeader()
        {
            if (_buffer.Length < MsgPack.HeadLen())
            {
                int length = 0;

                try
                {
                    length = await Socket.ReceiveAsync(_bytes, SocketFlags.None);
                }
                catch (Exception e)
                {
                    if (Socket == null)
                        return false;
                    
                    Log.Error($"Receive msg error: '{e.Message}'.");
                    
                    Shutdown();
                    
                    return false;
                }

                if (length == 0 && _buffer.Length == 0)
                    return true;
                
                if (length > 0)
                    _buffer.Write(_bytes, length);
            
                if (_buffer.Length < MsgPack.HeadLen())
                {
                    Log.Error($"Msg head length '{_buffer.Length}' not enough.");
                
                    ResetStream();

                    return true;
                }
            }

            MsgPack.UnPackHead(_buffer, IsEncrypt);
            
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
            
            Stopwatch stopwatch = new Stopwatch();
            
            if (_buffer.Length < contentLen)
            {
                int length = 0;

                try
                {
                    stopwatch.Start();
                    Log.Log($"CCC 【Begin Receive】");

                    length = await Socket.ReceiveAsync(_bytes, SocketFlags.None);
                    
                    stopwatch.Stop();
                    Log.Log($"CCC 【End Receive】 {length} {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (Exception e)
                {
                    if (Socket == null)
                        return false;

                    Log.Error($"Receive msg error: '{e.Message}'.");
                    
                    Shutdown();
                    
                    return false;
                }
                
                if (length > 0)
                    _buffer.Write(_bytes, length);
                else
                {
                    Log.Error($"Msg content length '{_buffer.Length}' not enough. Head set length is: '{contentLen}'");

                    ResetStream();
                    
                    _isHasData = false;
                    
                    return true;
                }

                if (_buffer.Length < contentLen)
                    return true;
            }

            _isHasData = false;

            stopwatch.Start();
            Log.Log($"CCC 【Begin Unpack】");
            
            var msg = MsgPack.UnPackContent(_buffer, IsEncrypt);
            
            stopwatch.Stop();
            Log.Log($"CCC 【End Unpack】 MsgId:{msg.MsgId} {stopwatch.ElapsedMilliseconds}ms");

            // 回调
            OnRecData?.Invoke(msg);

            return true;
        }
    }
}