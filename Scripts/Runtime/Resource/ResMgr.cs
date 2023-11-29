using System;
using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Timer;
using Engine.Scripts.Runtime.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Engine.Scripts.Runtime.Resource
{
    public partial class ResMgr : SingletonClass<ResMgr>, IManager
    {
        public static readonly string BUNDLE_ASSETS_PATH = "Assets/BundleAssets/";
        
        // 记录已加载的ab，键名为ab名
        private Dictionary<string, ABInfo> _abDic = new ();

        private LogGroup _log;
        
        private EResLoadMode _resLoadMode;

        public void Reset()
        {
        }

        public void Init(EResLoadMode resLoadMode)
        {
            _resLoadMode = resLoadMode;
            
            _log = new LogGroup("ResMgr");

            // 注册定时器
            TimerMgr.Ins.UseLateUpdate(OnTimer);
            
            SpriteAtlasManager.atlasRequested += RequestAtlas;
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
            
            abInfo = new ABInfo(EABState.SyncLoading);

            _abDic.TryAdd(abName, abInfo);
            
            // 加载依赖
            LoadABDeps(abName);
            
            // 加载
            var ab = AssetBundle.LoadFromFile(abName);

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
            
            abInfo = new ABInfo(EABState.AsyncLoading);

            _abDic.TryAdd(abName, abInfo);
            
            // 加载依赖
            LoadABDepsAsync(abName, () =>
            {
                // 异步加载ab
                var req = AssetBundle.LoadFromFileAsync(abName);
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
                    if (abInfo.Value.Req.isDone)
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
                LoadAB(dep);
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
            
            foreach (var dep in deps)
            {
                LoadABAsync(dep, (ab) =>
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
            // todo 获得ab的依赖
            return new List<string>();
        }

        private string RelPath2ABName(string relPath)
        {
            // todo 根据相对路径获得ab名
            return "";
        }
    }
}