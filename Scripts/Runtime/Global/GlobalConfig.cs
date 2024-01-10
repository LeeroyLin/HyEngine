using System.IO;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Engine.Scripts.Runtime.Global
{
    public class GlobalConfig
    {
        /// <summary>
        /// 全局配置路径
        /// </summary>
        private static readonly string GLOBAL_CONFIG_STREAMING_ASSETS_PATH = "Config/GlobalConfig.json";
        private static readonly string GLOBAL_CONFIG_ASSET_PATH = "Assets/Settings/GlobalConfig.asset";

        public static GlobalConfigSO Conf;

        public static async Task LoadConf()
        {
            Conf = await LoadANewConf();
        }

        public static async Task<GlobalConfigSO> LoadANewConf()
        {
#if UNITY_EDITOR
                var conf = AssetDatabase.LoadAssetAtPath<GlobalConfigSO>(GLOBAL_CONFIG_ASSET_PATH);
                return conf;
#else
                var content = await ReadTextRuntime.ReadSteamingAssetsText(GLOBAL_CONFIG_STREAMING_ASSETS_PATH);
                var conf = JsonConvert.DeserializeObject<GlobalConfigSO>(content);
                return conf;
#endif
        }

        public static void SaveJsonFile(GlobalConfigSO conf)
        {
            var content = JsonConvert.SerializeObject(conf);
            File.WriteAllText(GLOBAL_CONFIG_STREAMING_ASSETS_PATH, content);
        }
    }
}