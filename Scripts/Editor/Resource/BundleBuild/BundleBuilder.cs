using System.Collections.Generic;
using System.IO;
using Client.Scripts.Runtime.Global;
using Client.Scripts.Runtime.Utils;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace Engine.Scripts.Editor.Resource.BundleBuild
{
    public class BundleBuilder
    {
        public static string CONFIG_PATH = $"{Application.dataPath}/BundleAssets/BundleConfig/BundleConfigData.json";
        public static string OUTPUT_PATH = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + "BundleOut";
        
        private static BundleConfig _config;
        
        // 资源对应ab名
        private static Dictionary<string, string> _assetABDic = new Dictionary<string, string>();
            
        // ab依赖的其他ab
        private static Dictionary<string, HashSet<string>> _abDepDic = new Dictionary<string, HashSet<string>>();

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
            Dictionary<string, BuildCompression> compressionDic = new Dictionary<string, BuildCompression>();

            GetAssetsFromConfig(buildInfos, compressionDic);
            
            // 检测资源依赖
            CheckAssetsDeps(buildInfos);
            
            // 循环依赖检测
            bool isLoopDep = CheckLoopDep();

            if (isLoopDep)
                return;
            
            var buildContent = new BundleBuildContent(buildInfos.ToArray());
            
            var outputPath = $"{OUTPUT_PATH}/{buildTarget.ToString()}/{GlobalConfig.Version}.{TimeUtil.GetLocalTimeMS() / 1000}";
            
            PathUtil.MakeSureDir(outputPath);
            
            var buildParams = new CustomBuildParameters(buildTarget, buildGroup, outputPath);

            buildParams.PerBundleCompression = compressionDic; 
            
            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out IBundleBuildResults results);
            
            // 保存目录文件
            SaveManifestFile(results, outputPath);
        }

        // 检测资源依赖
        static void CheckAssetsDeps(List<AssetBundleBuild> buildInfos)
        {
            _assetABDic.Clear();
            _abDepDic.Clear();
            
            foreach (var info in buildInfos)
            {
                foreach (var assetName in info.assetNames)
                {
                    _assetABDic.Add(assetName, info.assetBundleName);
                }
            }
            
            foreach (var info in buildInfos)
            {
                // 获得ab内每个资源
                foreach (var assetName in info.assetNames)
                {
                    var deps = AssetDatabase.GetDependencies(assetName);

                    // 获得对应资源的依赖资源
                    foreach (var dep in deps)
                    {
                        if (dep == assetName || dep.StartsWith("Packages/"))
                            continue;

                        // 依赖资源的ab
                        if (!_assetABDic.TryGetValue(dep, out var abName))
                            continue;

                        if (abName == info.assetBundleName)
                            continue;
                        
                        if (!_abDepDic.TryGetValue(info.assetBundleName, out var set))
                        {
                            set = new HashSet<string>();
                            _abDepDic.Add(info.assetBundleName, set);
                        }
                        
                        set.Add(abName);
                    }
                }
            }
        }

        // 保存目录文件
        static void SaveManifestFile(IBundleBuildResults results, string outputPath)
        {
            var savePath = $"{outputPath}/manifest.json";

            var dep = new ABManifest();

            foreach (var info in _abDepDic)
                dep.dependenceDic.Add(info.Key, new List<string>(info.Value));

            dep.config = _config;
            
            var content = JsonConvert.SerializeObject(dep);
            File.WriteAllText(savePath, content);
        }

        // 检测循环依赖
        static bool CheckLoopDep()
        {
            foreach (var info in _abDepDic)
            {
                var (isLoop, path) = CheckEachLoopDep(info.Value, info.Key);
                if (isLoop)
                {
                    var fullPath = $"{info.Key} -> {path}";
                    Debug.LogError($"【Bundle Builder】 Has loop dep. {fullPath}");

                    return true;
                }
            }

            return false;
        }

        // 检测单个ab的循环依赖
        static (bool, string) CheckEachLoopDep(HashSet<string> depABNames, string abName)
        {
            foreach (var depAbName in depABNames)
            {
                if (depAbName == abName)
                    return (true, abName);
                
                if (_abDepDic.TryGetValue(depAbName, out var set))
                {
                    var (isLoop, path) = CheckEachLoopDep(set, abName);
                    if (isLoop)
                    {
                        var fullPath = $"{depAbName} -> {path}";
                        return (true, fullPath);
                    }
                }
            }

            return (false, "");
        }

        private static void LoadConfig()
        {
            var content = File.ReadAllText(CONFIG_PATH);
            _config = JsonConvert.DeserializeObject<BundleConfig>(content);

            if (_config == null)
                Debug.LogError($"【Bundle Builder】 There is no BundleConfigData.json at {CONFIG_PATH}");
        }

        static void GetAssetsFromConfig(List<AssetBundleBuild> list, Dictionary<string, BuildCompression> compressionDic)
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

                        var abName = GetABName(data.path, data.md5);
                        list.Add(new AssetBundleBuild()
                        {
                            assetBundleName = abName,
                            assetNames = relPathList.ToArray(),
                        });
                        compressionDic.Add(abName, GetCompression(data.packCompressType));
                    }
                    break;
                    case EABPackDir.File:
                    {
                        var pathList = PathUtil.GetAllFilesAtPath(absPath);

                        foreach (var path in pathList)
                        {
                            var abName = GetABName($"{data.path}_{Path.GetFileNameWithoutExtension(path)}", data.md5);
                            list.Add(new AssetBundleBuild()
                            {
                                assetBundleName = abName,
                                assetNames = new string[]{PathUtil.AbsolutePath2AssetsPath(path)},
                            });
                            compressionDic.Add(abName, GetCompression(data.packCompressType));
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

                            var abName = GetABName($"{data.path}_{info.Name}", data.md5);
                            list.Add(new AssetBundleBuild()
                            {
                                assetBundleName = abName,
                                assetNames = relPathList.ToArray(),
                            });
                            compressionDic.Add(abName, GetCompression(data.packCompressType));
                        });
                    }
                    break;
                }
            }
        }
        
        static BuildCompression GetCompression(EABCompress compressType)
        {
            switch (compressType)
            {
                case EABCompress.LZ4:
                    return BuildCompression.LZ4;
                case EABCompress.LZMA:
                    return BuildCompression.LZMA;
            }
            
            return BuildCompression.Uncompressed;
        }

        static string GetABName(string oriABName, bool isMd5)
        {
            if (isMd5)
                return Md5.EncryptMD5_32(oriABName);

            return oriABName;
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