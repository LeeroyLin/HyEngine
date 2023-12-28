using System.Collections.Generic;
using System.IO;
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
        public static string CONFIG_PATH = $"{ResMgr.BUNDLE_ASSETS_PATH}BundleConfig/BundleConfigData.asset";
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

            GetAssetsFromConfig(buildInfos);
            
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
                Debug.LogError($"【Bundle Builder】 There is no BundleConfigData.asset at {ResMgr.BUNDLE_ASSETS_PATH}BundleConfig");
        }

        static void GetAssetsFromConfig(List<AssetBundleBuild> list)
        {
            foreach (var data in _config.dataList)
            {
                var relPath = $"{ResMgr.BUNDLE_ASSETS_PATH}{data.path}";
                var absPath = PathUtil.AssetsPath2AbsolutePath(relPath);

                switch (data.packDirType)
                {
                    case EABPackDir.Single:
                    {
                        var pathList = PathUtil.GetAllFilesAtPath(absPath);

                        var relPathList = new List<string>();
                        
                        foreach (var path in pathList)
                        {
                            relPathList.Add(PathUtil.AbsolutePath2AssetsPath(path));
                        }
                        
                        list.Add(new AssetBundleBuild()
                        {
                            assetBundleName = data.path,
                            assetNames = relPathList.ToArray(),
                        });
                    }
                    break;
                    case EABPackDir.File:
                    {
                        var pathList = PathUtil.GetAllFilesAtPath(absPath);

                        foreach (var path in pathList)
                        {
                            list.Add(new AssetBundleBuild()
                            {
                                assetBundleName = $"{data.path}_{Path.GetFileNameWithoutExtension(path)}",
                                assetNames = new string[]{PathUtil.AbsolutePath2AssetsPath(path)},
                            });
                        }
                    }
                    break;
                    case EABPackDir.SubSingle:
                    {
                        PathUtil.ForeachSubDirectoriesAtPath(absPath, true, info =>
                        {
                            var pathList = PathUtil.GetAllFilesAtPath(info.FullName);

                            var relPathList = new List<string>();
                        
                            foreach (var path in pathList)
                            {
                                relPathList.Add(PathUtil.AbsolutePath2AssetsPath(path));
                            }
                            
                            list.Add(new AssetBundleBuild()
                            {
                                assetBundleName = $"{data.path}_{info.Name}",
                                assetNames = relPathList.ToArray(),
                            });
                        });
                    }
                    break;
                }
            }
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