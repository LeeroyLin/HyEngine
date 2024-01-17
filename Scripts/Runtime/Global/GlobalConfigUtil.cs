using System;
using System.IO;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace Engine.Scripts.Runtime.Global
{
    public class GlobalConfigUtil
    {
        /// <summary>
        /// 全局配置路径
        /// </summary>
        private static readonly string GLOBAL_CONFIG_STREAMING_ASSETS_PATH = "Config/GlobalConfig.json";

        public static GlobalConfig Conf;

        public static async Task LoadConf()
        {
            Conf = await LoadANewConfRuntime();
        }

        public static async Task<GlobalConfig> LoadANewConfRuntime()
        {
            var content = await ReadTextRuntime.ReadSteamingAssetsText(GLOBAL_CONFIG_STREAMING_ASSETS_PATH);
            var conf = JsonConvert.DeserializeObject<GlobalConfig>(content);
            return conf;
        }

        public static GlobalConfig LoadANewConfEditor()
        {
#if UNITY_EDITOR
            var path = $"{Application.streamingAssetsPath}/GLOBAL_CONFIG_STREAMING_ASSETS_PATH";
            var content = File.ReadAllText(path);
            var conf = JsonConvert.DeserializeObject<GlobalConfig>(content);
            return conf;
#endif
        }

        public static bool SaveConf(GlobalConfig conf)
        {
#if UNITY_EDITOR
            var savePath = $"{Application.streamingAssetsPath}/GLOBAL_CONFIG_STREAMING_ASSETS_PATH";
            var content = JsonConvert.SerializeObject(conf);

            try
            {
                File.WriteAllText(savePath, content);
            }
            catch (Exception e)
            {
                Debug.LogError($"Save global config to {savePath} failed. err: {e.Message}");
                
                return false;
            }
#endif
            
            return true;
        }
    }
}