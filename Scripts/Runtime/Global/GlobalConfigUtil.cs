using System.Threading.Tasks;
using Engine.Scripts.Runtime.Utils;
using Newtonsoft.Json;

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
            Conf = await LoadANewConf();
        }

        public static async Task<GlobalConfig> LoadANewConf()
        {
            var content = await ReadTextRuntime.ReadSteamingAssetsText(GLOBAL_CONFIG_STREAMING_ASSETS_PATH);
            var conf = JsonConvert.DeserializeObject<GlobalConfig>(content);
            return conf;
        }
    }
}