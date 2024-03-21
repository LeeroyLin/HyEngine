using Engine.Scripts.Runtime.Global;
using UnityEditor;

namespace Engine.Scripts.Editor.Resource.BundleBuild
{
    public class BuildCmdConfig
    {
        public string name;
        public string version;
        public EEnv env;
        public BuildTarget platform;
        public bool isCompileAllCode;
        public bool isApk;
        public bool isAAB;
        public bool isDevBuild;
        public long time;
    }
}