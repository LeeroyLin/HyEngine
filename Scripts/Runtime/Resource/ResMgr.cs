using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Global;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Timer;
using Engine.Scripts.Runtime.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;

namespace Engine.Scripts.Runtime.Resource
{
    public partial class ResMgr : SingletonClass<ResMgr>, IManager
    {
        public static readonly string BUNDLE_ASSETS_PATH = "Assets/BundleAssets/";
        public static readonly string RUNTIME_BUNDLE_PATH = $"{Application.persistentDataPath}/{PlatformInfo.BuildTargetStr}";
        public static readonly string CONFIG_NAME = "manifest.json";
        private static readonly string RES_SERVER_PATH = "/Res";
        
        private static ABManifest _manifest;
        
        // 记录已加载的ab，键名为ab名
        private Dictionary<string, ABInfo> _abDic = new ();

        private LogGroup _log;
        
        private EResLoadMode _resLoadMode;

        private List<string> _removeList = new List<string>();

        public void Reset()
        {
        }

        public async Task Init(EResLoadMode resLoadMode)
        {
            Debug.Log("ResMgr InitMgr");
            
            _resLoadMode = resLoadMode;
            
            _log = new LogGroup("ResMgr");

            // 注册定时器
            TimerMgr.Ins.UseLateUpdate(OnTimer);
            
            SpriteAtlasManager.atlasRequested += RequestAtlas;

            await LoadManifest();
        }

        async Task LoadManifest()
        {
            if (GlobalConfigUtil.Conf.resLoadMode != EResLoadMode.AB)
                return;
            
            _log.Log("LoadManifest");

            string content = "";
            
            content = await ReadTextRuntime.ReadPersistentDataPathText($"{PlatformInfo.BuildTargetStr}/{CONFIG_NAME}");
            
            _manifest = JsonConvert.DeserializeObject<ABManifest>(content);

            if (_manifest == null)
                _log.Error("Can not find manifest file.");
        }
        
        private void RequestAtlas(string atlasName, Action<SpriteAtlas> callback)
        {
            SpriteAtlas atlas = null;
            if (_resLoadMode == EResLoadMode.Editor)
            {
                #if UNITY_EDITOR
                atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>($"{BUNDLE_ASSETS_PATH}Atlas/{atlasName}.spriteatlasv2");
                #endif
            }
            else if (_resLoadMode == EResLoadMode.Resource)
            {
                atlas = Resources.Load<SpriteAtlas>($"Atlas/{atlasName}.spriteatlasv2");
            }
            else
            {
                var abRelPath = $"Atlas/{atlasName}";
                
                // 加载ab
                var ab = LoadAB(abRelPath);

                // 加载资源
                atlas = ab.LoadAsset<SpriteAtlas>(atlasName);
            }
            
            callback(atlas);
        }

        /// <summary>
        /// 减少AB引用
        /// </summary>
        /// <param name="relPath">相对资源目录的资源路径</param>
        public void ReduceABRef(string relPath)
        {
            if (GlobalConfigUtil.Conf.resLoadMode != EResLoadMode.AB)
                return;
            
            // ab名
            var abName = RelPath2ABName(relPath, out var isInGame);

            if (_abDic.TryGetValue(abName, out var ab))
                ab.ReduceRef();
        }

        /// <summary>
        /// 同步加载ab
        /// </summary>
        /// <param name="relPath">相对资源目录的资源路径</param>
        /// <returns></returns>
        public AssetBundle LoadAB(string relPath)
        {
            if (_resLoadMode != EResLoadMode.AB)
                return null;
            
            if (relPath == null)
            {
                _log.Error("Can not sync load ab with null relative path.");
                return null;
            }
            
            if (relPath == "")
            {
                _log.Error("Can not sync load ab with empty relative path.");
                return null;
            }

            // ab名
            var abName = RelPath2ABName(relPath, out var isInGame);

            if (isInGame)
            {
                _log.Error("Can not sync load 'InGame' ab. Use async instead.");
                return null;
            }
            
            // 通过AB名加载AB
            return LoadABWithABName(abName);
        }

        /// <summary>
        /// 通过AB名加载AB
        /// </summary>
        /// <param name="abName"></param>
        /// <returns></returns>
        public AssetBundle LoadABWithABName(string abName)
        {
            if (_abDic.TryGetValue(abName, out var abInfo))
            {
                switch (abInfo.ABState)
                {
                    case EABState.Loaded:
                        return abInfo.AB;
                    case EABState.SyncLoading:
                        return null;
                    case EABState.AsyncLoading:
                        // 终止异步操作
                        var tempAB = abInfo.Req.assetBundle;
                        abInfo.Req = null;
                        break;
                }
            }
            else
            {
                abInfo = new ABInfo(EABState.SyncLoading);
                _abDic.Add(abName, abInfo);
            }
            
            // 加载依赖
            LoadABDeps(abName);

            var abPath = $"{RUNTIME_BUNDLE_PATH}/{abName}";
            
            // 加载
            AssetBundle ab = AssetBundle.LoadFromFile(abPath);

            abInfo.AB = ab;
            abInfo.ABState = EABState.Loaded;

            // 完成后的回调
            abInfo.OnLoaded?.Invoke(ab);
            abInfo.OnLoaded = null;

            return ab;
        }

        /// <summary>
        /// 异步加载ab
        /// </summary>
        /// <param name="relPath">相对资源目录的资源路径</param>
        /// <param name="callback">回调方法</param>
        public void LoadABAsync(string relPath, Action<AssetBundle> callback)
        {
            if (_resLoadMode != EResLoadMode.AB)
            {
                #if UNITY_EDITOR
                callback(null);
                #endif
                return;
            }
            
            if (relPath == null)
            {
                _log.Error("Can not async load ab with null relative path.");
                callback(null);
                return;
            }
            
            if (relPath == "")
            {
                _log.Error("Can not async load ab with empty relative path.");
                callback(null);
                return;
            }
            
            // ab名
            var abName = RelPath2ABName(relPath, out var isInGame);

            // 通过AB名，异步加载AB
            LoadABAsyncWithABName(abName, isInGame, callback);
        }

        /// <summary>
        /// 通过AB名，异步加载AB
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="isInGame"></param>
        /// <param name="callback"></param>
        public void LoadABAsyncWithABName(string abName, bool isInGame, Action<AssetBundle> callback)
        {
            var abPath = $"{RUNTIME_BUNDLE_PATH}/{abName}";

            if (_abDic.TryGetValue(abName, out var abInfo))
            {
                switch (abInfo.ABState)
                {
                    case EABState.Loaded:
                        callback(abInfo.AB);
                        return;
                    case EABState.SyncLoading:
                    case EABState.AsyncLoading:
                    case EABState.Downloading:
                    case EABState.Downloaded:
                        abInfo.OnLoaded += callback;
                        return;
                }
            }
            else
            {
                if (isInGame)
                {
                    // 是否需要更新
                    if (IsInGameABNeedUpdate(abPath, abName))
                    {
                        abInfo = new ABInfo(EABState.Downloading);
                        
                        // 更新
                        abInfo.DownloadReq = DownloadABFile(abName);
                    }
                    else
                        abInfo = new ABInfo(EABState.AsyncLoading);
                }
                else
                    abInfo = new ABInfo(EABState.AsyncLoading);
                
                abInfo.OnLoaded += callback;
                _abDic.Add(abName, abInfo);
            }

            // 加载依赖
            LoadABDepsAsync(abName, () =>
            {
                abInfo.IsDepLoaded = true;
                
                if (abInfo.ABState == EABState.Downloaded || abInfo.ABState == EABState.AsyncLoading)
                {
                    abInfo.ABState = EABState.AsyncLoading;
                    
                    // 异步加载ab
                    var req = AssetBundle.LoadFromFileAsync(abPath);
                    abInfo.Req = req;

                    _log.Log($"Start async load 'InGame' ab '{abName}'");
                }
            });
        }
        
        // 下载ab文件
        UnityWebRequestAsyncOperation DownloadABFile(string abName)
        {
            var netConf = GlobalConfigUtil.Conf.netConfig;
            
            var uri = $"{netConf.res.host}:{netConf.res.port}{RES_SERVER_PATH}/{PlatformInfo.BuildTargetStr}/{_manifest.version}/{abName}";
            
            var webRequest = UnityWebRequest.Get(uri);

            _log.Log($"Start download ab file '{uri}'");

            return webRequest.SendWebRequest();
        }

        // 是否该游戏内更新的ab，需要被更新
        bool IsInGameABNeedUpdate(string abPath, string abName)
        {
            if (!File.Exists(abPath))
                return true;
            
            // 获得本地文件md5
            string md5 = Md5.EncryptFileMD5_32(abPath);

            var info = _manifest.inGameFiles[abName];

            return md5 != info.md5;
        }

        private void OnTimer()
        {
            OnABTimer();
            OnAssetTimer();
        }

        private void OnABTimer()
        {
            _removeList.Clear();
            
            foreach (var abInfo in _abDic)
            {
                if (abInfo.Value.ABState == EABState.AsyncLoading)
                {
                    if (abInfo.Value.Req == null || !abInfo.Value.Req.isDone)
                        continue;

                    abInfo.Value.ABState = EABState.Loaded;
                    abInfo.Value.AB = abInfo.Value.Req.assetBundle;
                    abInfo.Value.Req = null;
                    
                    // 完成后的回调
                    abInfo.Value.OnLoaded?.Invoke(abInfo.Value.AB);
                    abInfo.Value.OnLoaded = null;
                }
                else if (abInfo.Value.ABState == EABState.Downloading)
                {
                    if (abInfo.Value.DownloadReq == null || !abInfo.Value.DownloadReq.isDone)
                        continue;

                    if (!string.IsNullOrEmpty(abInfo.Value.DownloadReq.webRequest.error))
                    {
                        _log.Error($"Download ab '{abInfo.Key}' file failed. err: {abInfo.Value.DownloadReq.webRequest.error}");
                        
                        abInfo.Value.DownloadReq.webRequest.Dispose();
                        
                        _removeList.Add(abInfo.Key);
                        
                        continue;
                    }

                    abInfo.Value.ABState = EABState.Downloaded;

                    _log.Log($"'InGame' ab '{abInfo.Key}' downloaded. Start save file.");

                    var data = abInfo.Value.DownloadReq.webRequest.downloadHandler.data;
                    
                    abInfo.Value.DownloadReq.webRequest.Dispose();

                    var savePath = $"{RUNTIME_BUNDLE_PATH}/{abInfo.Key}";
                    
                    try
                    {
                        File.WriteAllBytes(savePath, data);
                    }
                    catch (Exception e)
                    {
                        _log.Error($"Save ab '{abInfo.Key}' file failed. err: {e.Message}");
                        
                        _removeList.Add(abInfo.Key);
                        
                        continue;
                    }
                    
                    _log.Log($"'InGame' ab '{abInfo.Key}' saved.");

                    abInfo.Value.DownloadReq = null;

                    if (abInfo.Value.IsDepLoaded)
                    {
                        var abPath = $"{RUNTIME_BUNDLE_PATH}/{abInfo.Key}";
                        
                        abInfo.Value.ABState = EABState.AsyncLoading;
                        
                        // 异步加载ab
                        var req = AssetBundle.LoadFromFileAsync(abPath);
                        abInfo.Value.Req = req;

                        _log.Log($"Start async load 'InGame' ab '{abInfo.Key}'");
                    }
                }
            }

            foreach (var info in _removeList)
                _abDic.Remove(info);
        }

        /// <summary>
        /// 同步加载ab依赖
        /// </summary>
        /// <param name="abName"></param>
        private void LoadABDeps(string abName)
        {
            List<string> deps = GetABDeps(abName);

            foreach (var dep in deps)
            {
                LoadABWithABName(dep);
            }
        }
        
        /// <summary>
        /// 异步加载ab依赖
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="onLoaded"></param>
        private void LoadABDepsAsync(string abName, Action onLoaded)
        {
            List<string> deps = GetABDeps(abName);

            int length = deps.Count;
            int cnt = 0;

            if (length == 0)
                onLoaded();
            
            foreach (var dep in deps)
            {
                bool isInGame = _manifest.inGameFiles.ContainsKey(dep);
                
                LoadABAsyncWithABName(dep, isInGame, ab =>
                {
                    cnt++;

                    if (cnt == length)
                    {
                        onLoaded();
                    }
                });
            }
        }
        
        private List<string> GetABDeps(string abName)
        {
            if (_manifest.dependenceDic.TryGetValue(abName, out var depList))
                return depList;
            
            return new List<string>();
        }

        private string RelPath2ABName(string relPath, out bool isInGame)
        {
            int maxLength = 0;
            BundleConfigData data = null;

            isInGame = false;
            
            foreach (var config in _manifest.config.dataList)
            {
                if (relPath.StartsWith(config.path))
                {
                    if (maxLength == 0 || config.path.Length > maxLength)
                    {
                        maxLength = config.path.Length;
                        data = config;
                    }
                }
            }

            if (data == null)
            {
                _log.Error($"Can not get ab with relPath : {relPath}");
                return "";
            }

            isInGame = data.updateType == EABUpdate.InGame;

            switch (data.packDirType)
            {
                case EABPackDir.Single:
                    return GetABNameWithMd5(data.path, data.md5);
                case EABPackDir.File:
                {
                    var fileName = Path.GetFileNameWithoutExtension(relPath);

                    return GetABNameWithMd5($"{data.path}_{fileName}", data.md5);
                }
                case EABPackDir.SubSingle:
                {
                    relPath = relPath.Replace(data.path, "");
                    var strs = relPath.Split("/");
                    var dirName = "";
                    foreach (var str in strs)
                    {
                        if (string.IsNullOrEmpty(str))
                            continue;
                        
                        dirName = str;

                        break;
                    }

                    if (string.IsNullOrEmpty(dirName))
                    {
                        _log.Error($"Can not get ab use SubSingle with relPath : {relPath}");
                        return "";
                    }
                    
                    return GetABNameWithMd5($"{data.path}_{dirName}", data.md5);
                }
            }

            return "";
        }

        private string GetABNameWithMd5(string name, bool isMd5)
        {
            name = name.Replace("/", "__").Replace("\\", "__").Replace(".", "__");
            
            if (isMd5)
                return Md5.EncryptMD5_32(name);

            return name;
        }
    }
}