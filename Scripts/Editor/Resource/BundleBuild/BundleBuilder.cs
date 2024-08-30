using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Engine.Scripts.Runtime.Global;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.Utils;
using HybridCLR.Editor.Installer;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace Engine.Scripts.Editor.Resource.BundleBuild
{
    public partial class BundleBuilder
    {
        public static readonly string CONFIG_PATH = $"{Application.dataPath}/Settings/BundleConfigData.json";
        public static readonly string OUTPUT_PATH = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + "BundleOut";
        public static readonly string LOG_PATH = $"{Application.dataPath}/../BuildLogs";

        // 包内 ab资源 根目录
        private static readonly string PACKAGE_AB_DIR = $"{Application.streamingAssetsPath}/AB";
        // 包 版本路径
        private static readonly string APK_VERSION_PATH = $"{Application.streamingAssetsPath}/apk_version";
        
        // ab偏移
        private static readonly int AB_OFFSET = 500;
        
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
        
        // 记录ab的更新策略
        private static Dictionary<string, EABUpdate> _abUpdateDic = new Dictionary<string, EABUpdate>();

        // 图片资源 对应 图集 字典
        private static Dictionary<string, string> _imgAtlasDic = new Dictionary<string, string>();

        private static GlobalConfig _globalConfig;

        private static StringBuilder _sbLog = new StringBuilder();
        private static StringBuilder _sbAssets = new StringBuilder();

        private static BuildCmdConfig _buildCmdConfig;
        
        /// <summary>
        /// 通过命令打包
        /// </summary>
        /// <returns></returns>
        public static bool BuildWithCmd(BuildCmdConfig cmdConfig)
        {
            _buildCmdConfig = cmdConfig;
            
            // 检测是否安装热更
            CheckHybridCLRInstalled();
                
            return BuildBundle(_buildCmdConfig.platform, true, _buildCmdConfig.time);
        }

        // 检测是否安装热更
        static void CheckHybridCLRInstalled()
        {
            var installer = new InstallerController();
            if (!installer.HasInstalledHybridCLR())
                installer.InstallDefaultHybridCLR();
        }

        [MenuItem("Build/Bundle/Build/Android")]
        public static void BuildBundleAndroid()
        {
            LoadGlobalConfig();

            BuildBundle(BuildTarget.Android, false);
        }

        [MenuItem("Build/Bundle/Build/Win64")]
        public static void BuildBundleWin64()
        {
            LoadGlobalConfig();

            BuildBundle(BuildTarget.StandaloneWindows64, false);
        }

        public static bool BuildBundle(BuildTarget buildTarget, bool isCmd, long time = 0)
        {
            _sbLog.Clear();
            _sbAssets.Clear();
            
            var timestamp = time > 0 ? time : TimeUtilBase.GetLocalTimeMS() / 1000;
            
            LoadGlobalConfig();

            if (!LoadBundleConfig())
                return false;

            if (isCmd)
            {
                // 尝试通过命令行修改全局配置
                if (!TryChangeGlobalConfByCmdConf())
                    return false;
            }

            // 编译热更dlls
            if (!CompileHybridDlls(buildTarget))
                return false;
            
            // 从配置整理资源
            FormatAssetsFromConfig();
            
            // 是否有非法资源名
            if (_invalidAssetNames.Count > 0)
            {
                var value = string.Join("\n", _invalidAssetNames);
                LogError($"Has invalid assets.\n{value}");
                return false;
            }

            // 检测资源依赖
            CheckAssetsDeps();

            // 循环依赖检测
            if (CheckLoopDep())
                return false;
            
            // 循环游戏内更新的ab相关的依赖
            if (!CheckInGameDepOK())
                return false;

            // 开始打包
            if (!StartBuild(timestamp, buildTarget, GetTargetGroup(buildTarget)))
                return false;

            // 保存打包日志
            SaveLogFile(timestamp);

            return true;
        }

        static BuildTargetGroup GetTargetGroup(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return BuildTargetGroup.Android;
                case BuildTarget.iOS:
                    return BuildTargetGroup.iOS;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                    return BuildTargetGroup.Standalone;
            }
            
            return BuildTargetGroup.Standalone;
        }

        // 尝试通过命令行修改全局配置
        static bool TryChangeGlobalConfByCmdConf()
        {
            bool isChanged = false;

            if (_globalConfig.version != _buildCmdConfig.version)
            {
                _globalConfig.version = _buildCmdConfig.version;
                isChanged = true;
            }
            
            if (_globalConfig.env != _buildCmdConfig.env)
            {
                _globalConfig.env = _buildCmdConfig.env;
                isChanged = true;
            }

            if (!isChanged)
                return true;

            return GlobalConfigUtil.SaveConf(_globalConfig);
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
            var outputPath = $"{OUTPUT_PATH}/{buildTarget}/{finalVersion}";
            
            PathUtil.MakeSureDir(outputPath);
            
            var buildParams = new CustomBuildParameters(buildTarget, buildGroup, outputPath);
            
            buildParams.PerBundleCompression = _compressionDic; 
            
            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out IBundleBuildResults results);
            
            Log($"Build finished with exit code : {exitCode}");
            
            if (exitCode != ReturnCode.Success)
                return false;

            // 加密ab
            if (!EncryptAB(results))
                return false;
            
            // 保存目录文件
            if (!SaveManifestFile(outputPath, results, finalVersion))
                return false;

            // 记录包版本文件
            if (!SaveApkVersionFile(APK_VERSION_PATH, finalVersion))
                return false;

            // 移动包内资源
            if (!MovePackageFiles(buildTarget, results, outputPath))
                return false;

            return true;
        }

        /// <summary>
        /// 加密ab
        /// </summary>
        /// <returns></returns>
        static bool EncryptAB(IBundleBuildResults results)
        {
            byte[] bytes = null;
            byte[] newBytes = null;

            foreach (var info in results.BundleInfos)
            {
                try
                {
                    bytes = File.ReadAllBytes(info.Value.FileName);
                }
                catch (Exception e)
                {
                    LogError($"[EncryptAB] Read file at {info.Value.FileName} failed. err: {e.Message}");
                    return false;
                }

                newBytes = new byte[bytes.Length + AB_OFFSET];

                for (int i = 0; i < newBytes.Length; i++)
                {
                    if (i < AB_OFFSET)
                        newBytes[i] = bytes[i];
                    else
                        newBytes[i] = bytes[i - AB_OFFSET];
                }

                try
                {
                    File.WriteAllBytes(info.Value.FileName, newBytes);
                }
                catch (Exception e)
                {
                    LogError($"[EncryptAB] Save file at {info.Value.FileName} failed. err: {e.Message}");
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// 移动包内资源
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="results"></param>
        /// <param name="outputPath"></param>
        /// <returns></returns>
        static bool MovePackageFiles(BuildTarget buildTarget, IBundleBuildResults results, string outputPath)
        {
            PathUtil.MakeSureDir(PACKAGE_AB_DIR);
            PathUtil.ClearDir(PACKAGE_AB_DIR);
            
            var packageABDir = $"{PACKAGE_AB_DIR}/{buildTarget}";
            PathUtil.MakeSureDir(packageABDir);

            var pre = outputPath + "/";

            foreach (var info in results.BundleInfos)
            {
                var abName = info.Value.FileName.Replace(pre, "");
                
                var updateType = _abUpdateDic[abName];

                if (updateType != EABUpdate.Package)
                    continue;

                var path = $"{packageABDir}/{abName}";

                try
                {
                    File.Move(info.Value.FileName, path);
                }
                catch (Exception e)
                {
                    LogError($"Move file from {info.Value.FileName} to {path} failed. err: {e.Message}");

                    return false;
                }
            }

            return true;
        }
        
        // 保存apk版本文件
        static bool SaveApkVersionFile(string outputPath, string finalVersion)
        {
            Log("Save apk version file.");

            try
            {
                File.WriteAllText(outputPath, finalVersion);
            }
            catch (Exception e)
            {
                LogError($"Same apk version file failed. err : {e.Message}");

                return false;
            }
            
            return true;
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
                
                var abName = info.Value.FileName.Replace(pre, "");

                var updateType = _abUpdateDic[abName];

                List<ABManifestFile> files = null;
                
                switch (updateType)
                {
                    case EABUpdate.Advance:
                        dep.advanceFiles.Add(new ABManifestFile()
                        {
                            fileName = abName,
                            md5 = md5,
                        });
                        break;
                    case EABUpdate.Package:
                        dep.packageFiles.Add(new ABManifestFile()
                        {
                            fileName = abName,
                            md5 = md5,
                        });
                        break;
                    case EABUpdate.InGame:
                        dep.inGameFiles.Add(abName, new ABManifestFile()
                        {
                            fileName = abName,
                            md5 = md5,
                        });
                        break;
                }
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

                        var realDep = dep;

                        // 是否是图集图片
                        if (_imgAtlasDic.TryGetValue(dep, out var atlasPath))
                            realDep = atlasPath;
                        
                        // 依赖资源的ab
                        if (!_assetABDic.TryGetValue(realDep, out var abName))
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

        // 检测游戏内更新的ab包的引用
        static bool CheckInGameDepOK()
        {
            foreach (var info in _abUpdateDic)
            {
                // 是否是游戏内更新
                if (info.Value == EABUpdate.InGame)
                    continue;
                
                if (!_abDepDic.TryGetValue(info.Key, out var deps))
                    continue;

                foreach (var dep in deps)
                {
                    // 是否依赖了游戏内更新的ab
                    if (_abUpdateDic[dep] == EABUpdate.InGame)
                    {
                        LogError($"ab {info.Key} which is not 'InGame', dependence the 'InGame' ab {dep}.");

                        return false;
                    }
                }
            }

            return true;
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

        private static void LoadGlobalConfig()
        {
            _globalConfig = GlobalConfigUtil.LoadANewConfEditor();
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
            _abUpdateDic.Clear();
            _imgAtlasDic.Clear();
            
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
                            CheckAtlasImg(path);
                            relPathList.Add(PathUtil.AbsolutePath2AssetsPath(path));
                        }

                        var abName = GetABName(data.path, data.md5);
                        _buildInfos.Add(new AssetBundleBuild()
                        {
                            assetBundleName = abName,
                            assetNames = relPathList.ToArray(),
                        });
                        _compressionDic.Add(abName, GetCompression(data.packCompressType));
                        _abUpdateDic.Add(abName, data.updateType);

                        Append2AssetsSB(abName, relPathList);
                    }
                    break;
                    case EABPackDir.File:
                    {
                        var pathList = PathUtil.GetAllFilesAtPath(absPath);

                        foreach (var path in pathList)
                        {
                            CheckAssetNameAvailableAndRecord(path);
                            CheckAtlasImg(path);
                            
                            var abName = GetABName($"{data.path}_{Path.GetFileNameWithoutExtension(path)}", data.md5);
                            List<string> assetNames = new List<string>() { PathUtil.AbsolutePath2AssetsPath(path) };
                            _buildInfos.Add(new AssetBundleBuild()
                            {
                                assetBundleName = abName,
                                assetNames = assetNames.ToArray(),
                            });
                            _compressionDic.Add(abName, GetCompression(data.packCompressType));
                            _abUpdateDic.Add(abName, data.updateType);
                            
                            Append2AssetsSB(abName, assetNames);
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
                                CheckAtlasImg(path);
                                relPathList.Add(PathUtil.AbsolutePath2AssetsPath(path));
                            }

                            var abName = GetABName($"{data.path}_{info.Name}", data.md5);
                            _buildInfos.Add(new AssetBundleBuild()
                            {
                                assetBundleName = abName,
                                assetNames = relPathList.ToArray(),
                            });
                            _compressionDic.Add(abName, GetCompression(data.packCompressType));
                            _abUpdateDic.Add(abName, data.updateType);
                            
                            Append2AssetsSB(abName, relPathList);
                        });
                    }
                    break;
                }
            }
        }

        static void Append2AssetsSB(string abName, List<string> assetsList)
        {
            _sbAssets.Append("【AB】: ");
            _sbAssets.AppendLine(abName);

            foreach (var assetName in assetsList)
                _sbAssets.AppendLine(assetName);   
        }

        // 检测资源名，并记录非法资源名
        private static void CheckAssetNameAvailableAndRecord(string assetName)
        {
            var name = Path.GetFileNameWithoutExtension(assetName);
            if (!IsAssetNameAvailable(name))
                _invalidAssetNames.Add(assetName);
        }

        // 检测图集图片
        private static void CheckAtlasImg(string assetName)
        {
            var relPath = PathUtil.AbsolutePath2AssetsPath(assetName);
            
            var extension = Path.GetExtension(relPath);

            if (extension != ".spriteatlasv2")
                return;

            var deps = AssetDatabase.GetDependencies(relPath);

            foreach (var dep in deps)
            {
                if (dep == assetName)
                    continue;
                
                _imgAtlasDic.Add(dep, relPath);
            }
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
            _sbLog.AppendLine($"【Log】 {msg}");
            Debug.Log($"【Bundle Builder】 {msg}");
        }

        static void LogError(string msg)
        {
            _sbLog.AppendLine($"【ERROR】 {msg}");
            Debug.LogError($"【Bundle Builder】 {msg}");
        }

        static void SaveLogFile(long timestamp)
        {
            Log("Save log file.");

            var dir = $"{LOG_PATH}/{timestamp}";
            
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var logPath = $"{dir}/Log_{timestamp}.txt";
            File.WriteAllText(logPath, _sbLog.ToString());

            var assetPath = $"{dir}/Assets_{timestamp}.txt";
            File.WriteAllText(assetPath, _sbAssets.ToString());

            _sbLog.Clear();
            _sbAssets.Clear();
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