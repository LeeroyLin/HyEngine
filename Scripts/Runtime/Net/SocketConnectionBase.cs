using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Log;
using UnityEngine;

namespace Engine.Scripts.Runtime.Net
{
    struct MsgQueueData
    {
        public NetMsg NetMsg { get; set; }
        public object UserData { get; set; }
    }

    struct SendingMsgInfo
    {
        public int MsgId { get; private set; }
        public float SendAt { get; private set; }

        public SendingMsgInfo(int msgId)
        {
            MsgId = msgId;
            SendAt = Time.time;
        }
    }
    
    public abstract class SocketConnectionBase : ISocketConnection
    {
        public string Key { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public Socket Socket { get; private set; }
        public bool IsEncrypt { get; private set; }
        public float MsgTimeout { get; private set; }
        
        public Action<string> OnConnected { get; set; }
        public Action<string> OnConnectFailed { get; set; }
        public Action<string> OnDisconnected { get; set; }
        public Action<string> OnShutdown { get; set; }
        public Action<bool> OnSending { get; set; }
        public Action<NetMsg, object> OnSendData { get; set; }
        public Action<NetMsg> OnRecData { get; set; }
        public IConnMsgPack MsgPack { get; protected set; }
        public LogGroup Log { get; protected set; }
        
        public int MaxMsgContentLen { get; protected set; }
        public ushort MsgId { get; private set; }

        byte[] _bytes = new byte[1024 * 10];
        
        QueueBuffer _buffer = new QueueBuffer();
        
        private bool _isHasData = false;

        private Queue<MsgQueueData> _sendMsgQueue = new Queue<MsgQueueData>();
        
        /// <summary>
        /// 断线后 备份未发送完毕的消息
        /// </summary>
        private Queue<MsgQueueData> _backupMsgQueue = new Queue<MsgQueueData>();
        private bool _isSending = false;

        /// <summary>
        /// 记录发送中未回复的msgId和时间
        /// </summary>
        private Dictionary<int, SendingMsgInfo> _sendingMsgId = new Dictionary<int, SendingMsgInfo>();

        /// <summary>
        /// 记录消息超时的回调字典
        /// 用于外部注册
        /// </summary>
        private Dictionary<float, List<Action<int>>> _msgTimeOutDic = new Dictionary<float, List<Action<int>>>();
        
        public SocketConnectionBase(string host, int port, IConnMsgPack msgPack, bool isEncrypt, int maxMsgContentLen, float msgTimeout)
        {
            Key = GetKey(host, port);
            
            Log = new LogGroup(Key);
            
            Host = host;
            Port = port;
            MsgPack = msgPack;
            IsEncrypt = isEncrypt;
            MaxMsgContentLen = maxMsgContentLen;
            
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.SendTimeout = 10000;
            Socket.ReceiveTimeout = 10000;
            MsgId = 0;
            MsgTimeout = msgTimeout;
        }

        public static string GetKey(string host, int port)
        {
            return $"Conn {host}:{port}";
        }

        public async void Connect()
        {
            _sendingMsgId.Clear();
            
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

            StartCheckSendingOverTime();

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
            
            _msgTimeOutDic.Clear();
            
            OnShutdown?.Invoke(Key);
        }

        /// <summary>
        /// 是否有上次未发送成功备份的消息
        /// </summary>
        /// <returns></returns>
        public bool HasBackupMsg()
        {
            return _backupMsgQueue.Count > 0;
        }

        /// <summary>
        /// 开始发送上次未发送成功备份的消息
        /// </summary>
        public void SendBackupMsg()
        {
            while (_backupMsgQueue.Count > 0)
                _sendMsgQueue.Enqueue(_backupMsgQueue.Dequeue());

            TrySendMsg();
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

            // 判断tcp连接失效
            if (!IsSocketAvailable())
            {
                _backupMsgQueue.Clear();

                // 备份后续未发送的消息
                while (_sendMsgQueue.Count > 0)
                    _backupMsgQueue.Enqueue(_sendMsgQueue.Dequeue());
                
                _sendMsgQueue.Clear();
                
                return;
            }

            var data = _sendMsgQueue.Dequeue();
            
            var bytes = MsgPack.Pack(data.NetMsg, IsEncrypt);
            
            _isSending = true;

            _sendingMsgId.Add(data.NetMsg.MsgId, new SendingMsgInfo(data.NetMsg.MsgId));

            if (_sendingMsgId.Count > 0)
                OnSending?.Invoke(true);
            
            try
            {
                await Socket.SendAsync(bytes, SocketFlags.None);
            }
            catch (Exception e)
            {
                Log.Error($"Send message failed. {e.Message}");

                // 判断tcp连接失效
                if (!IsSocketAvailable())
                {
                    _backupMsgQueue.Clear();
                    
                    // 备份当前失败消息
                    _backupMsgQueue.Enqueue(data);

                    // 备份后续未发送的消息
                    while (_sendMsgQueue.Count > 0)
                        _backupMsgQueue.Enqueue(_sendMsgQueue.Dequeue());
                
                    _sendMsgQueue.Clear();
                }
            }
            
            _isSending = false;
            
            OnSendData?.Invoke(data.NetMsg, data.UserData);

            TrySendMsg();
        }

        bool IsSocketAvailable()
        {
            return Socket != null && Socket.Connected;
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
                await Task.Yield();

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

        async void StartCheckSendingOverTime()
        {
            while (Socket != null && Socket.Connected)
            {
                await Task.Yield();
                
                foreach (var kv in _sendingMsgId)
                {
                    var expire = Time.time - kv.Value.SendAt;                    
                    
                    // 消息超时
                    if (expire >= MsgTimeout)
                    {
                        Log.Log($"{Host}:{Port} sending over time {MsgTimeout}.");
                    
                        _backupMsgQueue.Clear();
                    
                        // 备份后续未发送的消息
                        while (_sendMsgQueue.Count > 0)
                            _backupMsgQueue.Enqueue(_sendMsgQueue.Dequeue());
                
                        _sendMsgQueue.Clear();
                    
                        Shutdown();
                    
                        break;
                    }

                    TryCallTimeOutCb(kv.Value.MsgId, expire);
                }
            }
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
            
            if (_buffer.Length < contentLen)
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

            var msg = MsgPack.UnPackContent(_buffer, IsEncrypt);
            
            _sendingMsgId.Remove(msg.MsgId);
            if (_sendingMsgId.Count == 0)
                OnSending?.Invoke(false);
            
            // 回调
            OnRecData?.Invoke(msg);

            return true;
        }

        protected void RegMsgTimeOutCb(float time, Action<int> callback)
        {
            if (!_msgTimeOutDic.TryGetValue(time, out var list))
            {
                list = new List<Action<int>>();
                _msgTimeOutDic.Add(time, list);
            }
            
            list.Add(callback);
        }

        void TryCallTimeOutCb(int msgId, float expireTime)
        {
            foreach (var kv in _msgTimeOutDic)
            {
                if (expireTime > kv.Key)
                {
                    foreach (var action in kv.Value)
                        action?.Invoke(msgId);
                }
            }
        }
    }
}