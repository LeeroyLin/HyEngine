using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Utils;
using Newtonsoft.Json;
using UnityEditor;
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
        
        private static readonly string GLOBAL_CONFIG_KEY = "��f��awԎx�x;B@�n�?�R��u���YC�Q����H���@)D8?{E�ʬ�@k<*<y�����]���T~�d`v����X���6��0/�3Z����^�VQ1~�l���<�wmC�`=�-����.�;i>�ܓ�jg�vZ�X�5vO<����_�wrޏT�;ʇ�zMe��h��{�f`���q���O̲C]�����t��S�N�]��)���)�x�1�H������JV-;��8����{38��qj���>]�(��4A����ge��Հ���-��>R޹�Ň���PեG/������S�a��vL���׾,��U�gp�`�ئQ��T�c�HLF�Ƴ��*���g��U���0�iiU��3d�����D4߾U4U��{G��s�v��=�l�M���/�o�����k�x�f�Խ�O�Pr��D.5�G��n�����^�Y)^�^�����>���QOd����J���e������O�ɠ�g���[ɸSF��T�ѥ�g�4ad��s��+ɼ��t��)����vԌ-|�����s��/-p�(e�(�ź�y>����b��w`�㶀�ģ����@�6��v|�grA�Ǡxơ����8H�L�gES�G8�]�H�_i�bb�����o;�h��W�`";

        public static async Task LoadConf()
        {
            Conf = await LoadANewConfRuntime();
        }

        public static async Task<GlobalConfig> LoadANewConfRuntime()
        {
            var content = await ReadTextRuntime.ReadSteamingAssetsText(GLOBAL_CONFIG_STREAMING_ASSETS_PATH);
            
            var bytes = Encoding.UTF8.GetBytes(content);

            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(bytes[i] ^ GLOBAL_CONFIG_KEY[i % GLOBAL_CONFIG_KEY.Length]);

            var str = Encoding.UTF8.GetString(bytes);
            
            var conf = JsonConvert.DeserializeObject<GlobalConfig>(str);
            return conf;
        }

        public static GlobalConfig LoadANewConfEditor()
        {
#if UNITY_EDITOR
            var path = $"{Application.streamingAssetsPath}/{GLOBAL_CONFIG_STREAMING_ASSETS_PATH}";
            var content = File.ReadAllText(path);
            var conf = JsonConvert.DeserializeObject<GlobalConfig>(content);
            return conf;
#endif

            return null;
        }

        public static bool SaveConf(GlobalConfig conf)
        {
#if UNITY_EDITOR
            var savePath = $"{Application.streamingAssetsPath}/{GLOBAL_CONFIG_STREAMING_ASSETS_PATH}";
            var content = JsonConvert.SerializeObject(conf);

            PathUtil.MakeSureDir(Path.GetDirectoryName(savePath));
            
            var bytes = Encoding.UTF8.GetBytes(content);

            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(bytes[i] ^ GLOBAL_CONFIG_KEY[i % GLOBAL_CONFIG_KEY.Length]);
            
            try
            {
                File.WriteAllBytes(savePath, bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"Save global config to {savePath} failed. err: {e.Message}");
                
                return false;
            }
#endif
            
            Debug.Log($"Save global config finished.");
            
            return true;
        }
    }
}