using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Global;
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
        
        private static BundleConfig _bundleConfig;
        
        // 资源对应ab名
        private static Dictionary<string, string> _assetABDic = new Dictionary<string, string>();
            
        // ab依赖的其他ab
        private static Dictionary<string, HashSet<string>> _abDepDic = new Dictionary<string, HashSet<string>>();
        
        // 构件信息
        private static List<AssetBundleBuild> _buildInfos = new List<AssetBundleBuild>();
        
        // 非法资源名
        private static List<string> _invalidAssetNames = new List<string>();
        
        // 压缩字典
        private static Dictionary<string, BuildCompression> _compressionDic = new Dictionary<string, BuildCompression>();

        private static GlobalConfigSO _globalConfig;
        
        [MenuItem("Bundle/Build/Android")]
        public static async void Build()
        {
            await LoadGlobalConfig();
            
            LoadBundleConfig();
            
            Debug.Log("【Bundle Builder】 Start build bundle via Android platform.");
            
            // 从配置整理资源
            FormatAssetsFromConfig();
            
            // 是否有非法资源名
            if (_invalidAssetNames.Count > 0)
            {
                var value = string.Join("\n", _invalidAssetNames);
                Debug.Log($"【Bundle Builder】 Has invalid assets.\n{value}");
                return;
            }
            
            // 检测资源依赖
            CheckAssetsDeps();
            
            // 循环依赖检测
            if (CheckLoopDep())
                return;

            // 开始打包
            StartBuild(BuildTarget.Android, BuildTargetGroup.Android);

            // 保存全局配置文件
            SaveGlobalConfigFile();
            
            Debug.Log("【Bundle Builder】 Build finished.");
        }

        /// <summary>
        /// 开始打包
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="buildGroup"></param>
        static void StartBuild(BuildTarget buildTarget, BuildTargetGroup buildGroup)
        {
            var buildContent = new BundleBuildContent(_buildInfos.ToArray());
            
            var outputPath = $"{OUTPUT_PATH}/{buildTarget.ToString()}/{_globalConfig.version}.{TimeUtilBase.GetLocalTimeMS() / 1000}";
            
            PathUtil.MakeSureDir(outputPath);
            
            var buildParams = new CustomBuildParameters(buildTarget, buildGroup, outputPath);

            buildParams.PerBundleCompression = _compressionDic; 
            
            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out IBundleBuildResults results);
            
            // 保存目录文件
            SaveManifestFile(outputPath, results);
        }

        // 保存目录文件
        static void SaveManifestFile(string outputPath, IBundleBuildResults results)
        {
            var savePath = $"{outputPath}/manifest.json";

            var dep = new ABManifest();

            foreach (var info in _abDepDic)
                dep.dependenceDic.Add(info.Key, new List<string>(info.Value));

            dep.config = _bundleConfig;

            dep.version = _globalConfig.version;

            var pre = outputPath + "/";
            
            foreach (var info in results.BundleInfos)
            {
                dep.files.Add(new ABManifestFile()
                {
                    fileName = info.Value.FileName.Replace(pre, ""),
                    crc = info.Value.Crc,
                });
            }
            
            var content = JsonConvert.SerializeObject(dep);
            File.WriteAllText(savePath, content);
        }
        
        /// <summary>
        /// 保存全局配置文件
        /// </summary>
        static void SaveGlobalConfigFile()
        {
            GlobalConfig.SaveJsonFile(_globalConfig);
        }

        // 检测资源依赖
        static void CheckAssetsDeps()
        {
            _assetABDic.Clear();
            _abDepDic.Clear();
            
            foreach (var info in _buildInfos)
            {
                foreach (var assetName in info.assetNames)
                {
                    _assetABDic.Add(assetName, info.assetBundleName);
                }
            }
            
            foreach (var info in _buildInfos)
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

        private static async Task LoadGlobalConfig()
        {
            _globalConfig = await GlobalConfig.LoadANewConf();
        }

        private static void LoadBundleConfig()
        {
            var content = File.ReadAllText(CONFIG_PATH);
            _bundleConfig = JsonConvert.DeserializeObject<BundleConfig>(content);

            if (_bundleConfig == null)
                Debug.LogError($"【Bundle Builder】 There is no BundleConfigData.json at {CONFIG_PATH}");
        }

        /// <summary>
        /// 从配置整理资源
        /// </summary>
        static void FormatAssetsFromConfig()
        {
            _buildInfos.Clear();
            _invalidAssetNames.Clear();
            _compressionDic.Clear();
            
            foreach (var data in _bundleConfig.dataList)
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
                            CheckAssetNameAvailableAndRecord(path);
                            relPathList.Add(PathUtil.AbsolutePath2AssetsPath(path));
                        }

                        var abName = GetABName(data.path, data.md5);
                        _buildInfos.Add(new AssetBundleBuild()
                        {
                            assetBundleName = abName,
                            assetNames = relPathList.ToArray(),
                        });
                        _compressionDic.Add(abName, GetCompression(data.packCompressType));
                    }
                    break;
                    case EABPackDir.File:
                    {
                        var pathList = PathUtil.GetAllFilesAtPath(absPath);

                        foreach (var path in pathList)
                        {
                            CheckAssetNameAvailableAndRecord(path);
                            
                            var abName = GetABName($"{data.path}_{Path.GetFileNameWithoutExtension(path)}", data.md5);
                            _buildInfos.Add(new AssetBundleBuild()
                            {
                                assetBundleName = abName,
                                assetNames = new string[]{PathUtil.AbsolutePath2AssetsPath(path)},
                            });
                            _compressionDic.Add(abName, GetCompression(data.packCompressType));
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
                                CheckAssetNameAvailableAndRecord(path);
                                relPathList.Add(PathUtil.AbsolutePath2AssetsPath(path));
                            }

                            var abName = GetABName($"{data.path}_{info.Name}", data.md5);
                            _buildInfos.Add(new AssetBundleBuild()
                            {
                                assetBundleName = abName,
                                assetNames = relPathList.ToArray(),
                            });
                            _compressionDic.Add(abName, GetCompression(data.packCompressType));
                        });
                    }
                    break;
                }
            }
        }

        // 检测资源名，并记录非法资源名
        private static void CheckAssetNameAvailableAndRecord(string assetName)
        {
            var name = Path.GetFileNameWithoutExtension(assetName);
            if (!IsAssetNameAvailable(name))
                _invalidAssetNames.Add(assetName);
        }

        // 是否资源名有效
        private static bool IsAssetNameAvailable(string name)
        {
            Regex reg = new Regex(@"^[a-zA-Z0-9_]+$");

            return reg.IsMatch(name);
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