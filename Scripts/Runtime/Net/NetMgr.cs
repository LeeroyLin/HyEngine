using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Google.Protobuf;

namespace Engine.Scripts.Runtime.Net
{
    public partial class NetMgr : ManagerBase<NetMgr>
    {
        /// <summary>
        /// 是否有主连接
        /// </summary>
        public bool MainConnConnected {
            get
            {
                if (string.IsNullOrEmpty(_mainConnKey))
                    return false;

                var conn = _connDic[_mainConnKey];
                return conn.Socket.Connected;
            }
        }
        
        private Dictionary<string, SocketConnectionBase> _connDic = new Dictionary<string, SocketConnectionBase>();

        private LogGroup _log;

        private string _mainConnKey;
        
        public NetMgr()
        {
            _log = new LogGroup("NetMgr");
        }
        
        public void Init()
        {
        }

        protected override void OnReset()
        {
            ShutdownAllConn();
        }

        protected override void OnDisposed()
        {
            ClearConnDic();
            _mainConnKey = null;
        }

        /// <summary>
        /// 添加主连接
        /// </summary>
        /// <param name="conn"></param>
        public void AddMainConn(SocketConnectionBase conn)
        {
            var key = conn.Key;
            _mainConnKey = key;
            _connDic.Add(key, conn);

            conn.OnShutdown += OnConnShutdown;
        }

        /// <summary>
        /// 获得主连接
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetMainConn<T>() where T : SocketConnectionBase
        {
            return _connDic[_mainConnKey] as T;
        }

        /// <summary>
        /// 添加连接
        /// </summary>
        /// <param name="conn"></param>
        public void AddConn(SocketConnectionBase conn)
        {
            var key = conn.Key;
            _connDic.Add(key, conn);

            conn.OnShutdown += OnConnShutdown;
        }

        /// <summary>
        /// 获得连接
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public SocketConnectionBase GetConn(string host, int port)
        {
            var key = SocketConnectionBase.GetKey(host, port);

            _connDic.TryGetValue(key, out var data);

            return data;
        }

        public void ShutdownAllConn()
        {
            var list = new List<SocketConnectionBase>();
            foreach (var info in _connDic)
                list.Add(info.Value);

            foreach (var conn in list)
                conn.Shutdown();
        }
        
        /// <summary>
        /// 通过主连接发送消息
        /// </summary>
        /// <param name="protoId"></param>
        /// <param name="bytes"></param>
        /// <param name="userData"></param>
        public void SendMsg(ushort protoId, byte[] bytes, object userData = null)
        {
            if (!MainConnConnected)
            {
                _log.Error("Not set main tcp connection.");
                return;   
            }

            _connDic[_mainConnKey].SendMsg(protoId, bytes, userData);
        }

        /// <summary>
        /// 通过主连接发送消息
        /// </summary>
        /// <param name="protoId"></param>
        /// <param name="message"></param>
        public void SendMsg(ushort protoId, IMessage message)
        {
            if (!MainConnConnected)
            {
                _log.Error("Not set main tcp connection.");
                return;   
            }
            
            _connDic[_mainConnKey].SendMsg(protoId, message.ToByteArray(), message);
        }

        void ClearConnDic()
        {
            var list = new List<SocketConnectionBase>();
            foreach (var info in _connDic)
                list.Add(info.Value);

            foreach (var conn in list)
                conn.Shutdown();
            
            _connDic.Clear();
        }
        
        void OnConnShutdown(string key)
        {
            _connDic.Remove(key);

            if (_mainConnKey == key)
                _mainConnKey = null;
        }
    }
}