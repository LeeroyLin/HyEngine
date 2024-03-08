using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace Engine.Scripts.Runtime.Resource
{
    struct AsyncLoadAssetWaiting
    {
        public AssetInfo Info { get; private set; }
        public string RelPath { get; private set; }
        public Func<AssetBundleRequest> GetAsyncReq { get; private set; }

        public AsyncLoadAssetWaiting(string relPath, AssetInfo info, Func<AssetBundleRequest> getAsyncReq)
        {
            RelPath = relPath;
            Info = info;
            GetAsyncReq = getAsyncReq;
        }
    }
    
    public partial class ResMgr
    {
        private Dictionary<string, AssetInfo> _assetDic = new Dictionary<string, AssetInfo>();

        // 最大异步加载资源数
        private static readonly int MAX_ASYNC_LOAD_ASSET_NUM = 5;
        
        // 异步加载资源，等待队列
        private List<AsyncLoadAssetWaiting> _asyncLoadAssetWaitingList = new List<AsyncLoadAssetWaiting>();

        private List<AssetInfo> _infos = new List<AssetInfo>();

        private int _asyncLoadAssetCnt = 0;
        
        private void OnAssetTimer()
        {
            _asyncLoadAssetCnt = 0;

            _infos.Clear();
            
            foreach (var info in _assetDic)
            {
                if (info.Value.AssetState == EAssetState.AsyncLoading)
                {
                    if (info.Value.Req == null)
                        continue;
                    
                    if (info.Value.Req.isDone)
                    {
                        if (info.Value.IsAtlas)
                        {
                            var atlas = info.Value.Asset as SpriteAtlas;
                            info.Value.Asset = atlas.GetSprite(info.Value.SpriteName);
                        }
                        else
                        {
                            info.Value.Asset = info.Value.Req.asset;
                        }

                        if (info.Value.Asset == null)
                            _log.Error($"Load asset '{info.Key}' failed.");
                        
                        info.Value.AssetState = EAssetState.Loaded;
                        info.Value.Req = null;

                        _infos.Add(info.Value);
                        
                        // 完成后的回调
                        info.Value.OnLoaded?.Invoke(info.Value.Asset);
                        info.Value.OnLoaded = null;
                    }
                    else
                    {
                        _asyncLoadAssetCnt++;
                    }
                }
            }

            foreach (var info in _infos)
            {
                info.OnLoaded?.Invoke(info.Asset);
                info.OnLoaded = null;
            }
        }

        private void CheckAsyncLoadAsset()
        {
            if (_asyncLoadAssetWaitingList.Count == 0)
                return;

            int left = MAX_ASYNC_LOAD_ASSET_NUM - _asyncLoadAssetCnt;
            
            if (left <= 0)
                return;

            int num = Mathf.Min(left, _asyncLoadAssetWaitingList.Count);

            for (int i = 0; i < num; i++)
            {
                var data = _asyncLoadAssetWaitingList[i];
                
                data.Info.AssetState = EAssetState.AsyncLoading;
                
                // 调用异步加载Asset
                data.Info.Req = data.GetAsyncReq?.Invoke();
            }
            
            _asyncLoadAssetWaitingList.RemoveRange(0, num);
        }
        
        public T GetAsset<T>(string relPath) where T: Object
        {
            bool isAtlas = IsRelPathAtlas(relPath, out var atlasName, out var spriteName);
            
            // 是否已经有
            if (_assetDic.TryGetValue(relPath, out var info))
            {
                switch (info.AssetState)
                {
                    case EAssetState.Loaded:
                        return AssetPost(info.Asset as T, relPath);
                    case EAssetState.SyncLoading:
                        return null;
                    case EAssetState.AsyncLoading:
                        // 终止异步操作
                        var tempAB = info.Req.asset;
                        info.Req = null;
                        break;
                    case EAssetState.AsyncWaiting:
                        for (int i = _asyncLoadAssetWaitingList.Count -1; i >= 0; i--)
                        {
                            if (_asyncLoadAssetWaitingList[i].RelPath == relPath)
                            {
                                // 移除等待列表
                                _asyncLoadAssetWaitingList.RemoveAt(i);
                                break;
                            }
                        }
                        break;
                }
            }
            else
            {
                info = new AssetInfo(EAssetState.SyncLoading, isAtlas);
                _assetDic.Add(relPath, info);
            }

            T newAsset = null;

            if (isAtlas)
                newAsset = TryGetSpriteFromAtlas<T>(atlasName, spriteName);
            else
            {
                if (_resLoadMode == EResLoadMode.Editor)
                {
                    #if UNITY_EDITOR
                    string p = $"{BUNDLE_ASSETS_PATH}{relPath}";
                    newAsset = AssetDatabase.LoadAssetAtPath<T>(p);
                    #endif
                }
                else if (_resLoadMode == EResLoadMode.Resource)
                {
                    newAsset = GetAssetFromResource<T>(relPath);
                }
                else
                {
                    // 加载ab
                    var ab = LoadAB(relPath);

                    // 获得资源名
                    var assetName = GetAssetNameWithRelativePath(relPath);
            
                    // 加载资源
                    newAsset = ab.LoadAsset<T>(assetName);
                }   
            }

            if (newAsset == null)
                _log.Error($"Load asset '{relPath}' failed.");
            
            info.Asset = newAsset;
            info.AssetState = EAssetState.Loaded;

            // 完成后的回调
            info.OnLoaded?.Invoke(newAsset);
            info.OnLoaded = null;
            
            return AssetPost(newAsset, relPath);
        }

        public void GetAssetAsync<T>(string relPath, Action<T> callback) where T : Object
        {
            bool isAtlas = IsRelPathAtlas(relPath, out var atlasName, out var spriteName);
            
            // 是否已经有
            if (_assetDic.TryGetValue(relPath, out var info))
            {
                switch (info.AssetState)
                {
                    case EAssetState.Loaded:
                        var obj = AssetPost(info.Asset as T, relPath);
                        callback?.Invoke(obj);
                        return;
                    case EAssetState.SyncLoading:
                    case EAssetState.AsyncLoading:
                    case EAssetState.AsyncWaiting:
                        info.OnLoaded += o =>
                        {
                            var obj = AssetPost(o as T, relPath);
                            callback?.Invoke(obj);
                        };
                        return;
                }
            }
            else
            {
                info = new AssetInfo(EAssetState.AsyncWaiting, isAtlas);
                info.OnLoaded += o =>
                {
                    var obj = AssetPost(o as T, relPath);
                    callback?.Invoke(obj);
                };
                _assetDic.Add(relPath, info);
            }
            
            if (isAtlas)
            {
                TryGetSpriteFromAtlasAsync<T>(atlasName, spriteName, info);
            }
            else
            {
                if (_resLoadMode == EResLoadMode.Editor)
                {
                    #if UNITY_EDITOR
                    var newAsset = AssetDatabase.LoadAssetAtPath<T>($"{BUNDLE_ASSETS_PATH}{relPath}");
                
                    info.Asset = newAsset;
                    info.AssetState = EAssetState.Loaded;

                    if (info.Asset == null)
                        _log.Error($"Load asset '{relPath}' failed.");

                    // 完成后的回调
                    if (info.OnLoaded != null)
                    {
                        info.OnLoaded(newAsset);
                        info.OnLoaded = null;
                    }
                    #endif
                }
                else if (_resLoadMode == EResLoadMode.Resource)
                {
                    var newAsset = GetAssetFromResource<T>(relPath);
                
                    info.Asset = newAsset;
                    info.AssetState = EAssetState.Loaded;

                    if (info.Asset == null)
                        _log.Error($"Load asset '{relPath}' failed.");

                    // 完成后的回调
                    if (info.OnLoaded != null)
                    {
                        info.OnLoaded(newAsset);
                        info.OnLoaded = null;
                    }
                }
                else
                {
                    // 加载ab
                    LoadABAsync(relPath, ab =>
                    {
                        // 添加到加载队列
                        _asyncLoadAssetWaitingList.Add(new AsyncLoadAssetWaiting(relPath, info, () =>
                        {
                            // 获得资源名
                            var assetName = GetAssetNameWithRelativePath(relPath);
                        
                            // 异步加载Asset
                            return ab.LoadAssetAsync<T>(assetName);
                        }));
                    });
                }
            }
        }

        public T GetAssetFromResource<T>(string relPath) where T: Object
        {
            var extension = Path.GetExtension(relPath);
            return Resources.Load<T>(relPath.Replace(extension, ""));
        }

        public Object GetAssetFromResource(string relPath, Type systemTypeInstance)
        {
            var extension = Path.GetExtension(relPath);
            var path = relPath.Replace(extension, "");
            return Resources.Load(path, systemTypeInstance);
        }
        
        T TryGetSpriteFromAtlas<T>(string atlasName, string spriteName) where T: Object
        {
            T asset = null;

            spriteName = Path.GetFileNameWithoutExtension(spriteName);
            
            if (_resLoadMode == EResLoadMode.Editor)
            {
                #if UNITY_EDITOR
                string atlasPath = $"{BUNDLE_ASSETS_PATH}Atlas/{atlasName}.spriteatlasv2";
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

                if (atlas == null)
                    _log.Error($"Can not load atlas : '{atlasPath}'");
                
                asset = atlas.GetSprite(spriteName) as T;
                #endif
            }
            else if (_resLoadMode == EResLoadMode.Resource)
            {
                var atlasPath = $"Atlas/{atlasName}.spriteatlasv2";
                var atlas = GetAssetFromResource<SpriteAtlas>(atlasPath);
                
                if (atlas == null)
                    _log.Error($"Can not load atlas : '{atlasPath}'");

                asset = atlas.GetSprite(spriteName) as T;
            }
            else
            {
                var abRelPath = $"Atlas/{atlasName}";
                
                // 加载ab
                var ab = LoadAB(abRelPath);

                // 加载资源
                var assetPath = $"{BUNDLE_ASSETS_PATH}{abRelPath}.spriteatlasv2";
                var atlas = ab.LoadAsset<SpriteAtlas>(assetPath);
                
                if (atlas == null)
                    _log.Error($"Can not load atlas : '{atlasName}'");

                asset = atlas.GetSprite(spriteName) as T;
            }

            return asset;
        }
        
        void TryGetSpriteFromAtlasAsync<T>(string atlasName, string spriteName, AssetInfo info) where T: Object
        {
            T asset = null;

            spriteName = Path.GetFileNameWithoutExtension(spriteName);

            if (_resLoadMode == EResLoadMode.AB || _resLoadMode == EResLoadMode.PackageAB)
            {
                var abRelPath = $"Atlas/{atlasName}";

                // 加载ab
                LoadABAsync(abRelPath, ab =>
                {
                    // 加载资源
                    ab.LoadAssetAsync<SpriteAtlas>(atlasName);
                });
            }
            else
            {
                asset = TryGetSpriteFromAtlas<T>(atlasName, spriteName);
                
                info.Asset = asset;
                info.AssetState = EAssetState.Loaded;

                if (info.Asset == null)
                    _log.Error($"Load atlas asset '{atlasName}' '{spriteName}' failed.");

                // 完成后的回调
                info.OnLoaded?.Invoke(asset);
                info.OnLoaded = null;
            }
        }

        private T AssetPost<T>(T asset, string relPath) where T: Object
        {
            if (typeof(T) == typeof(GameObject))
            {
                // 实例化
                GameObject obj = Object.Instantiate(asset as GameObject);

                var assetData = obj.AddComponent<AssetData>();
                assetData.relPath = relPath;

                return obj as T;
            }
            
            return asset;
        }

        /// <summary>
        /// 减少资源引用
        /// </summary>
        /// <param name="relPath"></param>
        private void ReduceAssetRef(string relPath)
        {
            ReduceABRef(relPath);
        }

        // 通过相对路径获得资源名
        private string GetAssetNameWithRelativePath(string relPath)
        {
            return $"{BUNDLE_ASSETS_PATH}{relPath}";
        }

        // 是否相对路径是读取图集
        private bool IsRelPathAtlas(string relPath, out string atlasName, out string spriteName)
        {
            atlasName = "";
            spriteName = "";
            
            if (!relPath.StartsWith("atlas://"))
            {
                return false;
            }

            var content = relPath.Replace("atlas://", "");

            var strs = content.Split(":");

            if (strs.Length < 2)
            {
                _log.Error($"Wrong atlas path: '{relPath}'");

                return false;
            }
            
            atlasName = strs[0];
            spriteName = strs[1];
            
            return true;
        }
    }
}