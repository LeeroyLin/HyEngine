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

            bool encryptFile = false;

            // 是否文件已处理过
            if (bytesReal[5] == 0x97 && bytesReal[6] == 0x2C)
            {
                if (bytesReal.Length >= 2048 + 6)
                {
                    if (bytesReal[1024 + 5] == 0x97 && bytesReal[1024 + 6] == 0x2C)
                        encryptFile = true;
                }
            }

            if (encryptFile)
            {
                // 处理过则不处理
                return;
            }
            
            // 假文件数据
            var bytesFake = new byte[bytesReal.Length / 2];
            
            for (int i = 0; i < bytesFake.Length; i++)
            {
                // 假文件随机修改内容
                if (i % 1024 == 0)
                    bytesFake[i] = 0xAF;
                else if (i % 1024 == 1)
                    bytesFake[i] = 0x1B;
                else if (i % 1024 == 2)
                    bytesFake[i] = 0xB1;
                else if (i % 1024 == 3)
                    bytesFake[i] = 0xFA;
                else if (i % 1024 == 4)
                {
                    if (Random.Range(0, 2) == 0)
                        bytesFake[i] = 0x18;
                    else
                        bytesFake[i] = 0x1D;
                }
                else if (i % 1024 == 5)
                    bytesFake[i] = 0x97;
                else if (i % 1024 == 6)
                    bytesFake[i] = 0x2C;
                else
                    bytesFake[i] = (byte) (bytesReal[i] ^ (byte)(Random.Range(1, 254) & 0xFF));
            }
            
            // 修改开头的AF1BB1FA
            bytesFake[0] = 0xAF;
            bytesFake[1] = 0x1B;
            bytesFake[2] = 0xB1;
            bytesFake[3] = 0xFA;
            
            // 修改开头的AF1BB1FA
            bytesReal[0] = 0x23;
            bytesReal[1] = 0xC5;
            bytesReal[2] = 0xD9;
            bytesReal[3] = 0x87;
            
            for (int i = 0; i < bytesReal.Length; i++)
            {
                if (i % 4 == 0)
                    bytesReal[i] = (byte) (bytesReal[i] ^ (byte)(i % 255) & 0xFF);
                else if (i % 4 == 1)
                    bytesReal[i] = (byte) (bytesReal[i] ^ (byte)(i % 188 + 15) & 0xFF);
                else if (i % 4 == 2)
                    bytesReal[i] = (byte) (bytesReal[i] ^ (byte)(i % 123 + 31) & 0xFF);
                else if (i % 4 == 3)
                    bytesReal[i] = (byte) (bytesReal[i] ^ (byte)(i % 98 + 41) & 0xFF);
            }
            
            File.WriteAllBytes(targetPath, bytesReal);
            File.WriteAllBytes(metadataPath, bytesFake);
        }
    }
}