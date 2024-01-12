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

            return await ReadText(filePath);
        }
        
        public static async Task<byte[]> ReadSteamingAssetsBytes(string streamingAssetsRelPath)
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, streamingAssetsRelPath);

            return await ReadBytes(filePath);
        }
        
        public static async Task<string> ReadPersistentDataPathText(string persistentDataPathRelPath)
        {
            string filePath = Path.Combine(Application.persistentDataPath, persistentDataPathRelPath);

            return await ReadText(filePath);
        }
        
        public static async Task<byte[]> ReadPersistentDataPathBytes(string persistentDataPathRelPath)
        {
            string filePath = Path.Combine(Application.persistentDataPath, persistentDataPathRelPath);

            return await ReadBytes(filePath);
        }
        
        static async Task<string> ReadText(string filePath)
        {
            // 根据平台调整路径格式
            if (Application.platform == RuntimePlatform.Android)
                filePath = "jar:file://" + filePath;
            else if (Application.platform == RuntimePlatform.OSXEditor)
                filePath = "file://" + filePath;

            UnityWebRequest request = UnityWebRequest.Get(filePath);
#if UNITY_2020_1_OR_NEWER
            await request.SendWebRequest();
#else
            await request.Send();
#endif

            if (request.result == UnityWebRequest.Result.Success)
                return Encoding.UTF8.GetString(request.downloadHandler.data);
            
            Debug.LogError($"[ReadText] failed. path: {filePath} err: {request.error}");
            return "";
        }
        
        static async Task<byte[]> ReadBytes(string filePath)
        {
            // 根据平台调整路径格式
            if (Application.platform == RuntimePlatform.Android)
                filePath = "jar:file://" + filePath;
            else if (Application.platform == RuntimePlatform.OSXEditor)
                filePath = "file://" + filePath;

            UnityWebRequest request = UnityWebRequest.Get(filePath);
#if UNITY_2020_1_OR_NEWER
            await request.SendWebRequest();
#else
            await request.Send();
#endif

            if (request.result == UnityWebRequest.Result.Success)
                return request.downloadHandler.data;
            
            Debug.LogError($"[ReadBytes] failed. path: {filePath} err: {request.error}");
            return null;
        }
    }
}