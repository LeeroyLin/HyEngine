using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace Engine.Scripts.Editor.Global
{
    public class BuildProcessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 0; } }
        
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            //输出打包后的Android工程路径
            Debug.Log($"Build processor android proj path : '{path}'");
            var metadataPath = $"{path}/src/main/assets/bin/Data/Managed/Metadata/global-metadata.dat";
            var targetPath = $"{path}/src/main/assets/bin/Data/level";
            
            if (!File.Exists(metadataPath))
            {
                Debug.Log($"Can not find global-metadata.dat");
                return;
            }
            
            Debug.Log($"Find global-metadata.dat. Encrypt.");

            var bytesReal = File.ReadAllBytes(metadataPath);
            var bytesFake = new byte[bytesReal.Length];

            for (int i = 0; i < bytesReal.Length; i++)
            {
                bytesFake[i] = (byte) (bytesReal[i] ^ (byte)(Random.Range(1, 254) & 0xFF));
                bytesReal[i] = (byte) (bytesReal[i] ^ 0xFF);
            }

            File.WriteAllBytes(targetPath, bytesReal);
            File.WriteAllBytes(metadataPath, bytesFake);
        }
    }
}