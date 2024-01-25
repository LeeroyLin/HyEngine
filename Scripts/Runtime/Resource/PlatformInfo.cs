using UnityEditor;
using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
    public class PlatformInfo
    {
        public static string BuildTargetStr
        {
            get
            {
                if (Application.isEditor)
                {
#if UNITY_EDITOR
                    var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
                    return activeBuildTarget.ToString();
#endif
                }

                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        return "Android";
                    case RuntimePlatform.IPhonePlayer:
                        return "iOS";
                    case RuntimePlatform.WindowsPlayer:
                        return "StandaloneWindows64";
                    case RuntimePlatform.OSXPlayer:
                        return "StandaloneOSX";
                }

                return "StandaloneWindows64";
            }
        }
    }
}