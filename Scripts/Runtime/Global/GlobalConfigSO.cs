using System;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Resource;
using UnityEngine;

namespace Engine.Scripts.Runtime.Global
{
    public enum EGlobalConfigLoginType
    {
        /// <summary>
        /// 帐号密码登录
        /// </summary>
        UsrPwd = 1,
        /// <summary>
        /// Tap登录，值随便写的
        /// </summary>
        TapTap = 10,
        /// <summary>
        /// 选择登录方式，值随便写的
        /// </summary>
        Choose = 100,
        /// <summary>
        /// 固定用户名登录，值随便写的
        /// </summary>
        FixedUsr = 101,
    }
    
    [CreateAssetMenu(fileName ="GlobalConfig", menuName ="ScriptableObject/NewGlobalConfig",order = 1 )]
    public class GlobalConfigSO : ScriptableObject
    {
        public EEnv env = EEnv.Develop;
        public EResLoadMode resLoadMode = EResLoadMode.AB;
        public int netMaxMsgLen = 1024 * 500; // 500k
        public bool isNetEncrypt = true;
        public ulong abOffset = 2000;
        // 开启服务器选择
        public bool isSelectServer = false;
        // 自动显示GM
        public bool autoShowGM = false;
        // 设置界面是否能打开GM
        public bool settingViewGM = false;
        // 自动显示调试
        public bool autoShowDebug = false;
        // 编辑器 防沉迷
        public bool editorAntiAddiction = false;
        // 包 防沉迷
        public bool packageAntiAddiction = false;
        // 防沉迷 测试模式
        public bool antiAddictionTestEnv = false;
        // 登录方式
        public EGlobalConfigLoginType loginType = EGlobalConfigLoginType.Choose;
        public string version = "0.1";
        public BuildConfig buildConfig;
        public LogConfig logConfig;
        public NetConfig netConfig;

        public EachNetConfig GetCurrNetConfig()
        {
            return netConfig.GetEnvNetConfig(env);
        }
    }
}