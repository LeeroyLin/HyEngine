using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    public partial class BundleBuilder
    {
        public static string CONFIG_PATH = $"{Application.dataPath}/BundleAssets/BundleConfig/BundleConfigData.json";
        public static string OUTPUT_PATH = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + "BundleOut";
        public static string LOG_PATH = $"{Application.dataPath}/../BuildLogs";
        public static string VERSION_FILE_SAVE_PATH = $"{Application.streamingAssetsPath}/Config/version.txt";
        
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

        private static StringBuilder _sb = new StringBuilder();
        
        [MenuItem("Bundle/Build/Android")]
        public static async void Build()
        {
            _sb.Clear();
            
            var buildTarget = BuildTarget.Android;
            var timestamp = TimeUtilBase.GetLocalTimeMS() / 1000;
            
            await LoadGlobalConfig();

            if (!LoadBundleConfig())
                return;
            
            // 编译热更dlls
            if (!CompileHybridDlls(buildTarget))
                return;

            // 从配置整理资源
            FormatAssetsFromConfig();
            
            // 是否有非法资源名
            if (_invalidAssetNames.Count > 0)
            {
                var value = string.Join("\n", _invalidAssetNames);
                LogError($"Has invalid assets.\n{value}");
                return;
            }
            
            // 检测资源依赖
            CheckAssetsDeps();
            
            // 循环依赖检测
            if (CheckLoopDep())
                return;

            // 开始打包
            if (!StartBuild(timestamp, buildTarget, BuildTargetGroup.Android))
                return;

            // 保存全局配置文件
            if (!SaveGlobalConfigFile())
                return;

            // 保存版本文件
            if (!SaveVersionFile())
                return;
            
            // 保存打包日志
            SaveLogFile(timestamp);
        }

        /// <summary>
        /// 开始打包
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="buildTarget"></param>
        /// <param name="buildGroup"></param>
        static bool StartBuild(long timestamp, BuildTarget buildTarget, BuildTargetGroup buildGroup)
        {
            Log($"Start build. platform: {buildTarget}");

            var buildContent = new BundleBuildContent(_buildInfos.ToArray());

            var finalVersion = $"{_globalConfig.version}.{timestamp}";
            var outputPath = $"{OUTPUT_PATH}/{buildTarget.ToString()}/{finalVersion}";
            
            PathUtil.MakeSureDir(outputPath);
            
            var buildParams = new CustomBuildParameters(buildTarget, buildGroup, outputPath);

            buildParams.PerBundleCompression = _compressionDic; 
            
            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out IBundleBuildResults results);
            
            Log($"Build finished with exit code : {exitCode}");

            if (exitCode != ReturnCode.Success)
                return false;
            
            // 保存目录文件
            return SaveManifestFile(outputPath, results, finalVersion);
        }

        // 保存目录文件
        static bool SaveManifestFile(string outputPath, IBundleBuildResults results, string finalVersion)
        {
            Log("Save manifest file.");

            var savePath = $"{outputPath}/manifest.json";

            var dep = new ABManifest();

            foreach (var info in _abDepDic)
                dep.dependenceDic.Add(info.Key, new List<string>(info.Value));

            dep.config = _bundleConfig;

            dep.version = finalVersion;

            var pre = outputPath + "/";

            HashSet<string> md5Hash = new HashSet<string>();

            foreach (var info in results.BundleInfos)
            {
                var md5 = Md5.EncryptFileMD5_32(info.Value.FileName);

                if (md5Hash.Contains(md5))
                {
                    LogError($"Same file md5. file : {info.Value.FileName}");
                    
                    return false;
                }

                md5Hash.Add(md5);
                
                dep.files.Add(new ABManifestFile()
                {
                    fileName = info.Value.FileName.Replace(pre, ""),
                    md5 = md5,
                });
            }
            
            var content = JsonConvert.SerializeObject(dep);

            try
            {
                File.WriteAllText(savePath, content);
            }
            catch (Exception e)
            {
                LogError($"Write manifest file failed. path: {savePath} err : {e.Message}");
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// 保存全局配置文件
        /// </summary>
        static bool SaveGlobalConfigFile()
        {
            Log("Save global config file.");

            try
            {
                GlobalConfig.SaveJsonFile(_globalConfig);
            }
            catch (Exception e)
            {
                LogError($"Save global config file failed. err : {e.Message}");
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// 保存版本文件
        /// </summary>
        static bool SaveVersionFile()
        {
            Log("Save version file.");

            try
            {
                File.WriteAllText(VERSION_FILE_SAVE_PATH, _globalConfig.version);
            }
            catch (Exception e)
            {
                LogError($"Save version file failed. err : {e.Message}");
                return false;
            }

            return true;
        }

        // 检测资源依赖
        static void CheckAssetsDeps()
        {
            Log("Check assets dependence.");

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
                    LogError($"Has loop dep. {fullPath}");

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

        private static bool LoadBundleConfig()
        {
            try
            {
                var content = File.ReadAllText(CONFIG_PATH);
                _bundleConfig = JsonConvert.DeserializeObject<BundleConfig>(content);
            }
            catch (Exception e)
            {
                LogError($"Load bundle config failed. path:{CONFIG_PATH} err : {e.Message}");
                return false;
            }
            
            if (_bundleConfig == null)
            {
                LogError($"There is no BundleConfigData.json at {CONFIG_PATH}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 从配置整理资源
        /// </summary>
        static void FormatAssetsFromConfig()
        {
            Log("Start format assets from config.");

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
            Regex reg = new Regex(@"^[a-zA-Z0-9_\-\.]+$");

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
            oriABName = oriABName.Replace("/", "__").Replace("\\", "__").Replace(".", "__");
            
            if (isMd5)
                return Md5.EncryptMD5_32(oriABName);

            return oriABName;
        }
        
        static void Log(string msg)
        {
            _sb.AppendLine($"【Log】 {msg}");
            Debug.Log($"【Bundle Builder】 {msg}");
        }

        static void LogError(string msg)
        {
            _sb.AppendLine($"【ERROR】 {msg}");
            Debug.LogError($"【Bundle Builder】 {msg}");
        }

        static void SaveLogFile(long timestamp)
        {
            Log("Save log file.");

            if (!Directory.Exists(LOG_PATH))
                Directory.CreateDirectory(LOG_PATH);

            var path = $"{LOG_PATH}/{timestamp}.txt";
            File.WriteAllText(path, _sb.ToString());
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