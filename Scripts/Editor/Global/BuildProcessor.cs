using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Engine.Scripts.Editor.Global
{
    public class BuildProcessor : IPostprocessBuildWithReport
    {
        int IOrderedCallback.callbackOrder { get { return 0; } }
        
        void IPostprocessBuildWithReport.OnPostprocessBuild( BuildReport report )
        {
            //输出打包后的Android工程路径
            Debug.Log($"Build processor android proj path : '{report.summary.outputPath}'");
            var metadataPath = $"{report.summary.outputPath}/unityLibrary/src/main/assets/bin/Data/Managed/Metadata/global-metadata.dat";
            var targetPath = $"{report.summary.outputPath}/unityLibrary/src/main/assets/bin/Data/Managed/Metadata/global-metadata2.dat";

            if (!File.Exists(metadataPath))
            {
                Debug.Log($"not find global-metadata.dat");
                return;
            }
            
            Debug.Log($"find global-metadata.dat");
            
            File.Move(metadataPath, targetPath);
        }
    }
}