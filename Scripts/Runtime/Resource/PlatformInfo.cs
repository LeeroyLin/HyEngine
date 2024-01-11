using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
    public class PlatformInfo
    {
        public static RuntimePlatform Platform => Application.isEditor ? RuntimePlatform.Android : Application.platform;
    }
}