using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;

namespace Engine.Scripts.Runtime.Net
{
    public class NetMgr : IManager
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

        public void Init(List<SocketConnectionBase> connList)
        {
            _connDic.Clear();
            foreach (var conn in connList)
            {
                var key = GetKey(conn);
                _connDic.Add(key, conn);
            }
        }

        void ClearConnDic()
        {
            foreach (var info in _connDic)
                info.Value.Shutdown();
            
            _connDic.Clear();
        }
        
        string GetKey(SocketConnectionBase conn)
        {
            return $"{conn.Host}:{conn.Port}";
        }
    }
}


// public class Client : MonoBehaviour
// {
//     private Socket _tcpClient;
//     private string _serverIP = "127.0.0.1";//服务器ip地址
//     private int _serverPort = 5000;//端口号
//     void Start()
//     {
//         //1、创建socket
//         _tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//
//         //2、建立一个连接请求
//         IPAddress iPAddress = IPAddress.Parse(_serverIP);
//         EndPoint endPoint = new IPEndPoint(iPAddress, _serverPort);
//         _tcpClient.Connect(endPoint);
//         Debug.Log("请求服务器连接");
//
//         //3、接受、发送消息
//         byte[] data = new byte[1024];
//         int length = _tcpClient.Receive(data);
//         var message = Encoding.UTF8.GetString(data, 0, length);
//         Debug.Log("客户端收到来自服务器发来的信息" + message);
//
//         //发送消息
//         string message2 = "Client Say To Hello";
//         _tcpClient.Send(Encoding.UTF8.GetBytes(message2));
//         Debug.Log("客户端向服务器发送消息" + message2);
//     }
// }