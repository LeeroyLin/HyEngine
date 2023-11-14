using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Scripts.Runtime.Resource
{
    public partial class ResMgr
    {
        private Dictionary<string, AssetInfo> _assetDic = new Dictionary<string, AssetInfo>();

        private void OnAssetTimer()
        {
            foreach (var info in _assetDic)
            {
                if (info.Value.AssetState == EAssetState.AsyncLoading)
                {
                    if (info.Value.Req.isDone)
                    {
                        info.Value.AssetState = EAssetState.Loaded;
                        info.Value.Asset = info.Value.Req.asset;
                        info.Value.Req = null;
                        
                        // 完成后的回调
                        info.Value.OnLoaded?.Invoke(info.Value.Asset);
                        info.Value.OnLoaded = null;
                    }
                }
            }
        }
        
        public T GetAsset<T>(string relPath) where T: Object
        {
            // 是否已经有
            if (_assetDic.TryGetValue(relPath, out var info))
            {
                switch (info.AssetState)
                {
                    case EAssetState.Loaded:
                        return AssetPost(info.Asset as T, relPath) as T;
                    case EAssetState.SyncLoading:
                        return null;
                    case EAssetState.AsyncLoading:
                        // 终止异步操作
                        var tempAB = info.Req.asset;
                        info.Req = null;
                        break;
                }
            }

            T newAsset = null;
            
            if (_resLoadMode == EResLoadMode.Editor)
            {
                #if UNITY_EDITOR
                string p = $"Assets\\BundleAssets\\{relPath}";
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

            info = new AssetInfo(EAssetState.SyncLoading);
            _assetDic.TryAdd(relPath, info);

            info.Asset = newAsset;
            info.AssetState = EAssetState.Loaded;

            // 完成后的回调
            info.OnLoaded?.Invoke(newAsset);
            info.OnLoaded = null;
            
            return AssetPost(newAsset, relPath);
        }

        public void GetAssetAsync<T>(string relPath, Action<T> callback) where T : Object
        {
            // 是否已经有
            if (_assetDic.TryGetValue(relPath, out var info))
            {
                switch (info.AssetState)
                {
                    case EAssetState.Loaded:
                        var obj = AssetPost(info.Asset, relPath) as T;
                        callback?.Invoke(obj);
                        return;
                    case EAssetState.SyncLoading:
                    case EAssetState.AsyncLoading:
                        info.OnLoaded += o=>{callback?.Invoke(o as T);};
                        break;
                }
            }
            
            info = new AssetInfo(EAssetState.AsyncLoading);
            _assetDic.TryAdd(relPath, info);
            
            if (_resLoadMode == EResLoadMode.Editor)
            {
                #if UNITY_EDITOR
                var newAsset = AssetDatabase.LoadAssetAtPath<T>($"Assets\\BundleAssets\\{relPath}");
                
                info.Asset = newAsset;
                info.AssetState = EAssetState.Loaded;

                // 完成后的回调
                info.OnLoaded?.Invoke(newAsset);
                info.OnLoaded = null;
                #endif
            }
            else if (_resLoadMode == EResLoadMode.Resource)
            {
                var newAsset = GetAssetFromResource<T>(relPath);
                
                info.Asset = newAsset;
                info.AssetState = EAssetState.Loaded;

                // 完成后的回调
                info.OnLoaded?.Invoke(newAsset);
                info.OnLoaded = null;
            }
            else
            {
                // 加载ab
                var ab = LoadAB(relPath);

                // 获得资源名
                var assetName = GetAssetNameWithRelativePath(relPath);
            
                // 异步加载Asset
                var req = ab.LoadAssetAsync<T>(assetName);
                info.Req = req;
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
            // todo
            return relPath;
        }
    }
}