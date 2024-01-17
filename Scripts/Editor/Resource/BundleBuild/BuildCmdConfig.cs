using Engine.Scripts.Runtime.Global;
using UnityEditor;

namespace Engine.Scripts.Editor.Resource.BundleBuild
{
    public class BuildCmdConfig
    {
        public string version;
        public EEnv env;
        public BuildTarget platform;
        public bool isCompileAllCode;
    }
}