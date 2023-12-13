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
        
        public NetMgr()
        {
            _log = new LogGroup("NetMgr");
        }
        
        public void Reset()
        {
            ClearConnDic();
        }

        public void Init(List<SocketConnectionBase> connList = null)
        {
            if (connList == null)
                return;
            
            _connDic.Clear();
            foreach (var conn in connList)
            {
                AddConn(conn);
            }
        }

        public void AddConn(SocketConnectionBase conn)
        {
            var key = GetKey(conn.Host, conn.Port);
            _connDic.Add(key, conn);
        }

        public void Dispose()
        {
            ClearConnDic();
        }

        public SocketConnectionBase GetConn(string host, int port)
        {
            var key = GetKey(host, port);
            return _connDic[key];
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