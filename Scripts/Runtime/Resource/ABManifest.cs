using System.Collections.Generic;

namespace Engine.Scripts.Runtime.Resource
{
    public class ABManifest
    {
        public Dictionary<string, List<string>> dependenceDic = new Dictionary<string, List<string>>();
        public BundleConfig config;
    }
}