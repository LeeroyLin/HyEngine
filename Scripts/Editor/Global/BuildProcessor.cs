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
            
            // 伪装的文件位置
            var targetPath = $"{path}/src/main/assets/bin/Data/Managed/Resources/System.dll-resources.dat";
            
            if (!File.Exists(metadataPath))
            {
                Debug.Log($"Can not find global-metadata.dat");
                return;
            }
            
            Debug.Log($"Find global-metadata.dat. Encrypt.");
            
            var bytesReal = File.ReadAllBytes(metadataPath);
            
            // 假文件数据
            var bytesFake = new byte[bytesReal.Length];

            for (int i = 0; i < bytesReal.Length; i++)
            {
                // 假文件随机修改内容
                if (i % 1024 == 0)
                    bytesFake[i] = 0xFA;
                else if (i % 1024 == 1)
                    bytesFake[i] = 0xB1;
                else if (i % 1024 == 2)
                    bytesFake[i] = 0x1B;
                else if (i % 1024 == 3)
                    bytesFake[i] = 0xAF;
                else
                    bytesFake[i] = (byte) (bytesReal[i] ^ (byte)(Random.Range(1, 254) & 0xFF));
            }
            
            // 修改开头的FAB11BAF
            bytesFake[0] = 0xFA;
            bytesFake[1] = 0x4E;
            bytesFake[2] = 0x1B;
            bytesFake[3] = 0x50;

            // // 修改开头的FAB11BAF
            // bytesReal[0] = 0x23;
            // bytesReal[1] = 0xC5;
            // bytesReal[2] = 0xD9;
            // bytesReal[3] = 0x87;
            //
            // for (int i = 0; i < bytesReal.Length; i++)
            // {
            //     bytesReal[i] = (byte) (bytesReal[i] ^ 0xFF);
            // }

            File.WriteAllBytes(targetPath, bytesReal);
            File.WriteAllBytes(metadataPath, bytesFake);
        }
    }
}