using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Resource;
using UnityEngine;

namespace Engine.Scripts.Runtime.Global
{
    [CreateAssetMenu(fileName ="GlobalConfig", menuName ="ScriptableObject/NewGlobalConfig",order = 1 )]
    public class GlobalConfigSO : ScriptableObject
    {
        public EEnv env = EEnv.Develop;
        public EResLoadMode resLoadMode = EResLoadMode.AB;
        public int netMaxMsgLen = 1024 * 500; // 500k
        public bool isNetEncrypt = true;
        public string version = "0.1";
        public LogConfig logConfig = new LogConfig();
        public NetConfig netConfig = new NetConfig();

        public EachNetConfig GetCurrNetConfig()
        {
            return netConfig.GetEnvNetConfig(env);
        }
    }
}