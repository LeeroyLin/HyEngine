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
        }
    }
}