using System;

namespace Engine.Scripts.Runtime.Global
{
    [Serializable]
    public class NetConfig
    {
        public EachNetConfig develop;
        public EachNetConfig release;
        public EachNetConfig production;

        public EachNetConfig GetEnvNetConfig(EEnv env)
        {
            switch (env)
            {
                case EEnv.Develop:
                    return develop;
                case EEnv.Release:
                    return release;
                case EEnv.Production:
                    return production;
            }

            return develop;
        }
    }

    [Serializable]
    public class EachNetConfig
    {
        public NetConfigHost login = new NetConfigHost();
        public NetConfigHost res = new NetConfigHost();
        public NetConfigHost record = new NetConfigHost();
    }

    [Serializable]
    public class NetConfigHost
    {
        public string host;
        public int port;
        public string path;
    }
}