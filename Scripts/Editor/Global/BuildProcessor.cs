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
            var targetPath = $"{path}/src/main/assets/bin/Data/Managed/Metadata/global-metadata2.dat";
            
            if (!File.Exists(metadataPath))
            {
                Debug.Log($"not find global-metadata.dat");
                return;
            }
            
            Debug.Log($"find global-metadata.dat");
            
            // File.Move(metadataPath, targetPath);
        }
    }
}