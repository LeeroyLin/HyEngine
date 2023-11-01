using System.Collections.Generic;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.Utils;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace Engine.Scripts.Editor.Resource.BundleBuild
{
    public partial  class BundleBuilder
    {
        public static string CONFIG_PATH = "Assets/BundleAssets/BundleConfig/BundleConfigData.asset";
        public static string OUTPUT_PATH = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + "BundleOut";
        
        private static BundleConfig _config;
        
        [MenuItem("Bundle/Build/Android")]
        public static void Build()
        {
            LoadConfig();
            
            Debug.Log("【Bundle Builder】 Start build bundle via Android platform.");
            
            StartBuild(BuildTarget.Android, BuildTargetGroup.Android);
            
            Debug.Log("【Bundle Builder】 Build finished.");
        }

        static void StartBuild(BuildTarget buildTarget, BuildTargetGroup buildGroup)
        {
            List<AssetBundleBuild> buildInfos = new List<AssetBundleBuild>();
            var buildContent = new BundleBuildContent(buildInfos.ToArray());

            var outputPath = $"{OUTPUT_PATH}\\{buildTarget.ToString()}";
            
            PathUtil.MakeSureDir(outputPath);
            
            var buildParams = new CustomBuildParameters(buildTarget, buildGroup, outputPath);
            
            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out IBundleBuildResults results);
        }

        private static void LoadConfig()
        {
            if (!_config)
                _config = AssetDatabase.LoadAssetAtPath<BundleConfig>(CONFIG_PATH);

            if (_config == null)
                Debug.LogError("【Bundle Builder】 There is no BundleConfigData.asset at Assets/BundleAssets/BundleConfig");
        }
    }
    
    class CustomBuildParameters : BundleBuildParameters
    {
        public Dictionary<string, UnityEngine.BuildCompression> PerBundleCompression { get; set; }

        public CustomBuildParameters(BuildTarget target, BuildTargetGroup group, string outputFolder) : base(target, group, outputFolder)
        {
            PerBundleCompression = new Dictionary<string, UnityEngine.BuildCompression>();
        }

        public override UnityEngine.BuildCompression GetCompressionForIdentifier(string identifier)
        {
            UnityEngine.BuildCompression value;
            if (PerBundleCompression.TryGetValue(identifier, out value))
                return value;
            return BundleCompression;
        }
    }
}