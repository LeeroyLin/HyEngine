using System;
using System.Collections.Generic;
using System.IO;
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
    // 异步加载ab 等待信息
    struct AsyncLoadABWaiting
    {
        public ABInfo Info { get; private set; }
        public string AbPath { get; private set; }
        public string AbName { get; private set; }

        public AsyncLoadABWaiting(string abPath, string abName, ABInfo info)
        {
            AbPath = abPath;
            AbName = abName;
            Info = info;
        }
    }

    public partial class ResMgr : ManagerBase<ResMgr>, IManager
    {
        public static readonly string BUNDLE_ASSETS_PATH = "Assets/BundleAssets/";
        public static readonly string RUNTIME_BUNDLE_PATH = $"{Application.persistentDataPath}/{PlatformInfo.BuildTargetStr}";
        public static readonly string PACKAGE_BUNDLE_PATH = $"{Application.streamingAssetsPath}/AB/{PlatformInfo.BuildTargetStr}";
        public static readonly string CONFIG_NAME = "manifest.json";
        
        // ab偏移
        private static readonly ulong AB_OFFSET = 500;
        
        // 最大异步加载ab数
        private static readonly int MAX_ASYNC_LOAD_AB_NUM = 5;
        
        // ab无引用后的销毁时间
        private static readonly int NO_REF_UNLOAD_TIME = 20;
        
        private static ABManifest _manifest;
        
        // 记录已加载的ab，键名为ab名
        private Dictionary<string, ABInfo> _abDic = new ();

        // 异步加载ab，等待队列
        private List<AsyncLoadABWaiting> _asyncLoadABWaitingList = new List<AsyncLoadABWaiting>();

        private LogGroup _log;
        
        private EResLoadMode _resLoadMode;

        private List<string> _removeList = new List<string>();

        private int _asyncLoadABCnt = 0;
        private List<ABInfo> _cbInfos = new List<ABInfo>();

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

        protected override void OnReset()
        {
            OnAssetReset();

            foreach (var data in _asyncLoadABWaitingList)
                data.Info.ReduceRef();
            
            _asyncLoadABWaitingList.Clear();
            _asyncLoadABCnt = 0;
            
            TimerMgr.Ins.RemoveLateUpdate(OnTimer);
            TimerMgr.Ins.UseLateUpdate(OnTimer);
        }

        protected override void OnDisposed()
        {
            OnAssetDisposed();
            
            _asyncLoadABWaitingList.Clear();
            _asyncLoadABCnt = 0;
            
            TimerMgr.Ins.RemoveLateUpdate(OnTimer);
        }
        
        async Task LoadManifest()
        {
            string content = null;

            if (GlobalConfigUtil.Conf.resLoadMode == EResLoadMode.AB)
            {
                _log.Log("LoadManifest");

                content = await ReadTextRuntime.ReadPersistentDataPathText($"{PlatformInfo.BuildTargetStr}/{CONFIG_NAME}");
            }
            else if (GlobalConfigUtil.Conf.resLoadMode == EResLoadMode.PackageAB)
            {
                _log.Log("LoadManifest");

                content = await ReadTextRuntime.ReadSteamingAssetsText($"AB/{PlatformInfo.BuildTargetStr}/{CONFIG_NAME}");
            }

            if (string.IsNullOrEmpty(content))
                return;
            
            _manifest = JsonConvert.DeserializeObject<ABManifest>(content);

            if (_manifest == null)
                _log.Error("Can not find manifest file.");
        }
        
        private void RequestAtlas(string atlasName, Action<SpriteAtlas> callback)
        {
            SpriteAtlas atlas = null;
            
            var assetName = $"Atlas/{atlasName}.spriteatlasv2";
            
            if (_resLoadMode == EResLoadMode.Editor)
            {
                #if UNITY_EDITOR
                atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>($"{BUNDLE_ASSETS_PATH}{assetName}");
                #endif
            }
            else if (_resLoadMode == EResLoadMode.Resource)
            {
                atlas = Resources.Load<SpriteAtlas>(assetName);
            }
            else
            {
                GetAssetAsync(assetName, callback);
                
                return;
            }
            
            callback(atlas);
        }

        /// <summary>
        /// 增加AB引用
        /// </summary>
        /// <param name="relPath">相对资源目录的资源路径</param>
        /// <param name="delta"></param>
        public void AddABRef(string relPath, int delta = 1)
        {
            if (GlobalConfigUtil.Conf.resLoadMode != EResLoadMode.AB && GlobalConfigUtil.Conf.resLoadMode != EResLoadMode.PackageAB)
                return;
            
            bool isAtlas = IsRelPathAtlas(relPath, out var atlasName, out var spriteName);

            if (isAtlas)
            {
                AddAtlasABRef(atlasName);
                return;
            }
            
            // ab名
            var abName = RelPath2ABName(relPath, out _, out _);

            if (_abDic.TryGetValue(abName, out var ab))
                ab.AddRef(delta);
        }

        /// <summary>
        /// 减少AB引用
        /// </summary>
        /// <param name="relPath">相对资源目录的资源路径</param>
        /// <param name="delta"></param>
        public void ReduceABRef(string relPath, int delta = 1)
        {
            if (GlobalConfigUtil.Conf.resLoadMode != EResLoadMode.AB && GlobalConfigUtil.Conf.resLoadMode != EResLoadMode.PackageAB)
                return;
            
            bool isAtlas = IsRelPathAtlas(relPath, out var atlasName, out var spriteName);

            if (isAtlas)
            {
                ReduceAtlasABRef(atlasName);
                return;
            }

            // ab名
            var abName = RelPath2ABName(relPath, out _, out _);

            if (_abDic.TryGetValue(abName, out var ab))
                ab.ReduceRef(delta);
        }

        /// <summary>
        /// 同步加载ab
        /// </summary>
        /// <param name="relPath">相对资源目录的资源路径</param>
        /// <returns></returns>
        public AssetBundle LoadAB(string relPath)
        {
            if (_resLoadMode != EResLoadMode.AB && _resLoadMode != EResLoadMode.PackageAB)
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
            var abName = RelPath2ABName(relPath, out var isInGame, out var isPackage);

            if (isInGame)
            {
                _log.Error("Can not sync load 'InGame' ab. Use async instead.");
                return null;
            }
            
            // 通过AB名加载AB
            return LoadABWithABName(abName, isPackage);
        }

        /// <summary>
        /// 通过AB名加载AB
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="isPackage"></param>
        /// <returns></returns>
        public AssetBundle LoadABWithABName(string abName, bool isPackage)
        {
            if (_abDic.TryGetValue(abName, out var abInfo))
            {
                switch (abInfo.ABState)
                {
                    case EABState.Loaded:
                        // 引用计数
                        abInfo.AddRef();
                        
                        return abInfo.AB;
                    case EABState.SyncLoading:
                        return null;
                    case EABState.AsyncLoading:
                        // 终止异步操作
                        var tempAB = abInfo.Req.assetBundle;
                        abInfo.Req = null;
                        break;
                    case EABState.AsyncWaiting:
                        for (int i = _asyncLoadABWaitingList.Count -1; i >= 0; i--)
                        {
                            if (_asyncLoadABWaitingList[i].AbName == abName)
                            {
                                // 移除等待列表
                                _asyncLoadABWaitingList.RemoveAt(i);
                                break;
                            }
                        }
                        break;
                }
            }
            else
            {
                abInfo = new ABInfo(EABState.SyncLoading);
                _abDic.Add(abName, abInfo);
            }
            
            // 加载依赖
            LoadABDeps(abName, isPackage);

            var abPath = $"{(isPackage ? PACKAGE_BUNDLE_PATH : RUNTIME_BUNDLE_PATH)}/{abName}";
            
            // 加载
            AssetBundle ab = AssetBundle.LoadFromFile(abPath, 0, AB_OFFSET);
                
            abInfo.AB = ab;
            abInfo.ABState = EABState.Loaded;
            
            // 引用计数
            abInfo.AddRef();

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
            if (_resLoadMode != EResLoadMode.AB && _resLoadMode != EResLoadMode.PackageAB)
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
            var abName = RelPath2ABName(relPath, out var isInGame, out var isPackage);

            // 通过AB名，异步加载AB
            LoadABAsyncWithABName(abName, isInGame, isPackage, callback);
        }

        /// <summary>
        /// 通过AB名，异步加载AB
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="isInGame"></param>
        /// <param name="isPackage"></param>
        /// <param name="callback"></param>
        public void LoadABAsyncWithABName(string abName, bool isInGame, bool isPackage, Action<AssetBundle> callback)
        {
            if (_resLoadMode != EResLoadMode.AB && _resLoadMode != EResLoadMode.PackageAB)
            {
#if UNITY_EDITOR
                callback(null);
#endif
                return;
            }

            var abPath = $"{(isPackage ? PACKAGE_BUNDLE_PATH : RUNTIME_BUNDLE_PATH)}/{abName}";

            if (_abDic.TryGetValue(abName, out var abInfo))
            {
                switch (abInfo.ABState)
                {
                    case EABState.Loaded:
                        // 引用计数
                        abInfo.AddRef();
                        
                        callback(abInfo.AB);
                        return;
                    case EABState.SyncLoading:
                    case EABState.AsyncLoading:
                    case EABState.AsyncWaiting:
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
                        abInfo = new ABInfo(EABState.AsyncWaiting);
                }
                else
                    abInfo = new ABInfo(EABState.AsyncWaiting);
                
                abInfo.OnLoaded += callback;
                _abDic.Add(abName, abInfo);
            }
            
            // 引用计数
            abInfo.AddRef();

            // 加载依赖
            LoadABDepsAsync(abName, isPackage, () =>
            {
                abInfo.IsDepLoaded = true;
                
                if (abInfo.ABState == EABState.Downloaded || abInfo.ABState == EABState.AsyncWaiting)
                {
                    abInfo.ABState = EABState.AsyncWaiting;
                    
                    // 添加到加载队列
                    _asyncLoadABWaitingList.Add(new AsyncLoadABWaiting(abPath, abName, abInfo));
                }
            });
        }
        
        // 下载ab文件
        UnityWebRequestAsyncOperation DownloadABFile(string abName)
        {
            var netConf = GlobalConfigUtil.Conf.netConfig;
            
            var uri = $"{netConf.res.host}:{netConf.res.port}{netConf.res.path}/{PlatformInfo.BuildTargetStr}/{_manifest.version}/{abName}";
            
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
            CheckLRU();
            
            OnABTimer();
            OnAssetTimer();

            CheckAsyncLoadAB();
            CheckAsyncLoadAsset();
        }

        private void OnABTimer()
        {
            _removeList.Clear();
            _cbInfos.Clear();
            _asyncLoadABCnt = 0;
            
            foreach (var abInfo in _abDic)
            {
                if (abInfo.Value.ABState == EABState.AsyncLoading)
                {
                    if (abInfo.Value.Req == null)
                        continue;

                    if (!abInfo.Value.Req.isDone)
                    {
                        _asyncLoadABCnt++;
                        
                        continue;
                    }

                    abInfo.Value.ABState = EABState.Loaded;
                    abInfo.Value.AB = abInfo.Value.Req.assetBundle;
                    abInfo.Value.Req = null;

                    _log.Log($"ab '{abInfo.Key}' async loaded.");
                    
                    _cbInfos.Add(abInfo.Value);
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
                        
                        abInfo.Value.ABState = EABState.AsyncWaiting;
                        
                        // 添加到异步队列
                        _asyncLoadABWaitingList.Add(new AsyncLoadABWaiting(abPath, abInfo.Key, abInfo.Value));
                    }
                }
            }

            foreach (var info in _removeList)
                _abDic.Remove(info);

            foreach (var info in _cbInfos)
            {
                info.OnLoaded?.Invoke(info.AB);
                info.OnLoaded = null;
            }
        }

        private void CheckAsyncLoadAB()
        {
            if (_asyncLoadABWaitingList.Count == 0)
                return;

            int left = MAX_ASYNC_LOAD_AB_NUM - _asyncLoadABCnt;
            
            if (left <= 0)
                return;

            int num = Mathf.Min(left, _asyncLoadABWaitingList.Count);

            for (int i = 0; i < num; i++)
            {
                var data = _asyncLoadABWaitingList[i];
                
                // 异步加载ab
                var req = AssetBundle.LoadFromFileAsync(data.AbPath, 0, AB_OFFSET);
                data.Info.ABState = EABState.AsyncLoading;
                data.Info.Req = req;
                
                _log.Log($"Start async load ab '{data.AbPath}'");
            }
            
            _asyncLoadABWaitingList.RemoveRange(0, num);
        }

        /// <summary>
        /// LRU处理ab销毁
        /// </summary>
        private void CheckLRU()
        {
            _removeList.Clear();
            
            foreach (var kv in _abDic)
            {
                if (kv.Value.RefCnt == 0 && Time.time - kv.Value.NoRefAt >= NO_REF_UNLOAD_TIME)
                {
                    // 记录销毁
                    _removeList.Add(kv.Key);
                }
            }

            foreach (var key in _removeList)
            {
                var info = _abDic[key];
                
                // 删除数据
                _abDic.Remove(key);
                
                if (!info.IsLoaded)
                    continue;
                
                // 卸载ab
                info.AB.Unload(true);
                RemoveABAssets(key);
            }
        }
        
        /// <summary>
        /// 同步加载ab依赖
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="isPackage"></param>
        private void LoadABDeps(string abName, bool isPackage)
        {
            List<string> deps = GetABDeps(abName);

            foreach (var dep in deps)
            {
                LoadABWithABName(dep, isPackage);
            }
        }
        
        /// <summary>
        /// 异步加载ab依赖
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="isPackage"></param>
        /// <param name="onLoaded"></param>
        private void LoadABDepsAsync(string abName, bool isPackage, Action onLoaded)
        {
            List<string> deps = GetABDeps(abName);

            int length = deps.Count;
            int cnt = 0;

            if (length == 0)
                onLoaded();
            
            foreach (var dep in deps)
            {
                bool isInGame = _manifest.inGameFiles.ContainsKey(dep);
                
                LoadABAsyncWithABName(dep, isInGame, isPackage, ab =>
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

        private string RelPath2ABName(string relPath, out bool isInGame, out bool isPackage)
        {
            int maxLength = 0;
            BundleConfigData data = null;

            isInGame = false;
            isPackage = false;
            
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
            isPackage = data.updateType == EABUpdate.Package;

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