using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Resource;

namespace Engine.Scripts.Runtime.Global
{
    public class GlobalConfig
    {
        public EEnv env = EEnv.Develop;
        public EResLoadMode resLoadMode = EResLoadMode.AB;
        public int netMaxMsgLen = 1024 * 500; // 500k
        public bool isNetEncrypt = true;
        public string version = "0.1";
        public LogConfig logConfig;
        public NetConfig netConfig;

        public EachNetConfig GetCurrNetConfig()
        {
            return netConfig.GetEnvNetConfig(env);
        }
    }
}