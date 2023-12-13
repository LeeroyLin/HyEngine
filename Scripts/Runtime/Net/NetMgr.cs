using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Utils;

namespace Engine.Scripts.Runtime.Net
{
    public partial class NetMgr : SingletonClass<NetMgr>, IManager
    {
        private Dictionary<string, SocketConnectionBase> _connDic = new Dictionary<string, SocketConnectionBase>();

        private LogGroup _log;

        private string _mainConnKey;
        
        public NetMgr()
        {
            _log = new LogGroup("NetMgr");
        }
        
        public void Reset()
        {
            ClearConnDic();
        }

        public void Init()
        {
        }

        /// <summary>
        /// 添加主连接
        /// </summary>
        /// <param name="conn"></param>
        public void AddMainConn(SocketConnectionBase conn)
        {
            var key = GetKey(conn.Host, conn.Port);
            _mainConnKey = key;
            _connDic.Add(key, conn);
        }

        /// <summary>
        /// 添加连接
        /// </summary>
        /// <param name="conn"></param>
        public void AddConn(SocketConnectionBase conn)
        {
            var key = GetKey(conn.Host, conn.Port);
            _connDic.Add(key, conn);
        }

        public void Dispose()
        {
            ClearConnDic();
        }

        /// <summary>
        /// 获得连接
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public SocketConnectionBase GetConn(string host, int port)
        {
            var key = GetKey(host, port);

            _connDic.TryGetValue(key, out var data);

            return data;
        }

        /// <summary>
        /// 通过主连接发送消息
        /// </summary>
        /// <param name="protoId"></param>
        /// <param name="bytes"></param>
        public void SendMsg(ushort protoId, byte[] bytes)
        {
            if (string.IsNullOrEmpty(_mainConnKey))
            {
                _log.Error("Not set main tcp connection.");
                return;   
            }

            _connDic[_mainConnKey].SendMsg(protoId, bytes);
        }

        void ClearConnDic()
        {
            foreach (var info in _connDic)
                info.Value.Shutdown();
            
            _connDic.Clear();
        }
        
        string GetKey(string host, int port)
        {
            return $"{host}:{port}";
        }
    }
}