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
        public ulong abOffset = 2000;
        // 登录方式
        public EGlobalConfigLoginType loginType;

        // 服务器手动选择
        public bool isSelectServer => flags[0];
        // 自动显示GM
        public bool autoShowGM => flags[1];
        // 设置界面是否能打开GM
        public bool settingViewGM => flags[2];
        // 自动显示调试
        public bool autoShowDebug => flags[3];
        // 编辑器 防沉迷
        public bool editorAntiAddiction => flags[4];
        // 包 防沉迷
        public bool packageAntiAddiction => flags[5];
        // 包 防沉迷
        public bool antiAddictionTestEnv => flags[6];
        
        public LogConfig logConfig;
        public EachNetConfig netConfig;
        public bool[] flags = new bool[7];

        public void SetFlags(bool isSelectServer,
            bool autoShowGM,
            bool settingViewGM,
            bool autoShowDebug,
            bool editorAntiAddiction,
            bool packageAntiAddiction,
            bool antiAddictionTestEnv)
        {
            flags[0] = isSelectServer;
            flags[1] = autoShowGM;
            flags[2] = settingViewGM;
            flags[3] = autoShowDebug;
            flags[4] = editorAntiAddiction;
            flags[5] = packageAntiAddiction;
            flags[6] = antiAddictionTestEnv;
        }
    }
}