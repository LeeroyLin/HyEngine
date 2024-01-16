using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Timer;
using Engine.Scripts.Runtime.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Engine.Scripts.Runtime.Resource
{
    public partial class ResMgr : SingletonClass<ResMgr>, IManager
    {
        public static readonly string BUNDLE_ASSETS_PATH = "Assets/BundleAssets/";
        public static readonly string RUNTIME_BUNDLE_PATH = $"{Application.persistentDataPath}/{PlatformInfo.Platform}";
        public static readonly string CONFIG_NAME = "manifest.json";
        
        private static ABManifest _manifest;
        
        // 记录已加载的ab，键名为ab名
        private Dictionary<string, ABInfo> _abDic = new ();

        private LogGroup _log;
        
        private EResLoadMode _resLoadMode;

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
            _log.Log("LoadManifest");

            string content = "";
            
            content = await ReadTextRuntime.ReadPersistentDataPathText($"{PlatformInfo.Platform}/{CONFIG_NAME}");
            
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
            // ab名
            var abName = RelPath2ABName(relPath);

            if (_abDic.TryGetValue(abName, out var ab))
            {
                ab.ReduceRef();
            }
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
            var abName = RelPath2ABName(relPath);

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
            var abName = RelPath2ABName(relPath);

            // 通过AB名，异步加载AB
            LoadABAsyncWithABName(abName, callback);
        }

        /// <summary>
        /// 通过AB名，异步加载AB
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="callback"></param>
        public void LoadABAsyncWithABName(string abName, Action<AssetBundle> callback)
        {
            if (_abDic.TryGetValue(abName, out var abInfo))
            {
                switch (abInfo.ABState)
                {
                    case EABState.Loaded:
                        callback(abInfo.AB);
                        return;
                    case EABState.SyncLoading:
                    case EABState.AsyncLoading:
                        abInfo.OnLoaded += callback;
                        return;
                }
            }
            else
            {
                abInfo = new ABInfo(EABState.AsyncLoading);
                _abDic.Add(abName, abInfo);
            }
            
            // 加载依赖
            LoadABDepsAsync(abName, () =>
            {
                var abPath = $"{RUNTIME_BUNDLE_PATH}/{abName}";

                // 异步加载ab
                var req = AssetBundle.LoadFromFileAsync(abPath);
                abInfo.Req = req;
            });
        }

        private void OnTimer()
        {
            OnABTimer();
            OnAssetTimer();
        }
        
        private void OnABTimer()
        {
            foreach (var abInfo in _abDic)
            {
                if (abInfo.Value.ABState == EABState.AsyncLoading)
                {
                    if (abInfo.Value.Req != null && abInfo.Value.Req.isDone)
                    {
                        abInfo.Value.ABState = EABState.Loaded;
                        abInfo.Value.AB = abInfo.Value.Req.assetBundle;
                        abInfo.Value.Req = null;
                        
                        // 完成后的回调
                        abInfo.Value.OnLoaded?.Invoke(abInfo.Value.AB);
                        abInfo.Value.OnLoaded = null;
                    }
                }
            }
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
                LoadABAsyncWithABName(dep, (ab) =>
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

        private string RelPath2ABName(string relPath)
        {
            int maxLength = 0;
            BundleConfigData data = null;
            
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