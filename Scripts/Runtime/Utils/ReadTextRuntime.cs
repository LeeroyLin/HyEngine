using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Engine.Scripts.Runtime.Utils
{
    public class ReadTextRuntime
    {
        public static async Task<string> ReadSteamingAssetsText(string streamingAssetsRelPath)
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, streamingAssetsRelPath);

            // 根据平台调整路径格式
            if (Application.platform == RuntimePlatform.Android)
            {
                filePath = "jar:file://" + filePath;
            }

            UnityWebRequest request = UnityWebRequest.Get(filePath);
#if UNITY_2020_1_OR_NEWER
            await request.SendWebRequest();
#else
            await request.Send();
#endif

            if (request.result == UnityWebRequest.Result.Success)
            {
                return Encoding.UTF8.GetString(request.downloadHandler.data);
            }
            else
            {
                Debug.LogError($"[LoadConfig] err: {request.error}");
                return "";
            }
        }
    }
}