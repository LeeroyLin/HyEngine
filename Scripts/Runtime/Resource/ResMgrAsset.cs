using System;
using System.Collections.Generic;
using System.IO;
using Engine.Scripts.Runtime.Global;
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

    struct AsyncInstantiateInfo
    {
        public string RelPath { get; private set; }
        public Action<GameObject> Callback { get; private set; }

        public AsyncInstantiateInfo(string relPath, Action<GameObject> callback)
        {
            RelPath = relPath;
            Callback = callback;
        }
    }
    
    public partial class ResMgr
    {
        // 资源路径 对应资源
        private Dictionary<string, AssetInfo> _assetDic = new Dictionary<string, AssetInfo>();

        // ab名 对应资源路径
        private Dictionary<string, HashSet<string>> _abAssetDic = new Dictionary<string, HashSet<string>>();

        // 最大异步加载资源数
        private static readonly int MAX_ASYNC_LOAD_ASSET_NUM = 5;

        // 最大异步实例化数量
        private static readonly int MAX_INSTANTIATE_CNT = 1000;
        
        // 异步加载资源，等待队列
        private List<AsyncLoadAssetWaiting> _asyncLoadAssetWaitingList = new List<AsyncLoadAssetWaiting>();
        
        // 异步实例化等待队列
        private static List<AsyncInstantiateInfo> _asyncInstantiateWaitingList = new List<AsyncInstantiateInfo>();

        private List<AssetInfo> _infos = new List<AssetInfo>();

        private int _asyncLoadAssetCnt = 0;

        /// <summary>
        /// 增加Asset引用
        /// </summary>
        /// <param name="relPath">相对资源目录的资源路径</param>
        /// <param name="delta"></param>
        public void AddAssetRef(string relPath, int delta = 1)
        {
            if (GlobalConfigUtil.Conf.resLoadMode != EResLoadMode.AB && GlobalConfigUtil.Conf.resLoadMode != EResLoadMode.PackageAB)
                return;
            
            if (_assetDic.TryGetValue(relPath, out var info))
                info.AddRef();
            
            AddABRef(relPath);
        }

        /// <summary>
        /// 减少Asset引用
        /// </summary>
        /// <param name="relPath">相对资源目录的资源路径</param>
        /// <param name="delta"></param>
        public void ReduceAssetRef(string relPath, int delta = 1)
        {
            if (GlobalConfigUtil.Conf.resLoadMode != EResLoadMode.AB && GlobalConfigUtil.Conf.resLoadMode != EResLoadMode.PackageAB)
                return;

            if (_assetDic.TryGetValue(relPath, out var info))
                info.ReduceRef();
            
            ReduceABRef(relPath);
        }
        
        protected void OnAssetReset()
        {
            foreach (var data in _asyncLoadAssetWaitingList)
            {
                if (data.Info.RefCnt > 0)
                    ReduceABRef(data.RelPath, data.Info.RefCnt);
                
                _assetDic.Remove(data.RelPath);
            }

            foreach (var data in _asyncInstantiateWaitingList)
            {
                if (_assetDic.TryGetValue(data.RelPath, out var info))
                    info.ReduceRef();
                
                ReduceABRef(data.RelPath);
            }
            
            _asyncLoadAssetWaitingList.Clear();
            _asyncInstantiateWaitingList.Clear();
            _asyncLoadAssetCnt = 0;
        }

        protected void OnAssetDisposed()
        {
            OnAssetReset();
        }
        
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

            var num = Mathf.Min(_asyncInstantiateWaitingList.Count, MAX_INSTANTIATE_CNT);

            for (int i = 0; i < num; i++)
            {
                var info = _asyncInstantiateWaitingList[0];
                _asyncInstantiateWaitingList.RemoveAt(0);
            
                if (!_assetDic.TryGetValue(info.RelPath, out var asset))
                    continue;

                if (asset.Asset == null)
                {
                    _log.Error($"Asset is null. Rel path: {info.RelPath}");
                    continue;
                }
                
                // 实例化
                GameObject obj = Object.Instantiate(asset.Asset as GameObject);
            
                var assetData = obj.AddComponent<AssetData>();
                assetData.relPath = info.RelPath;
            
                info.Callback(obj);
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
                // 增加资源引用
                info.AddRef();

                switch (info.AssetState)
                {
                    case EAssetState.Loaded:
                        // 增加ab引用
                        AddABRef(relPath);

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
                
                // 增加资源引用
                info.AddRef();
            }

            T newAsset = null;

            if (isAtlas)
                newAsset = TryGetSpriteFromAtlas<T>(atlasName, spriteName, relPath);
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
                    
                    RecordABAsset(ab.name, relPath);
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
                // 增加资源引用
                info.AddRef();
                
                // 增加ab引用
                AddABRef(relPath);

                switch (info.AssetState)
                {
                    case EAssetState.Loaded:
                    {
                        if (typeof(T) == typeof(GameObject))
                            _asyncInstantiateWaitingList.Add(new AsyncInstantiateInfo(relPath, (Action<GameObject>) callback));
                        else
                            callback(info.Asset as T);
                        return;
                    }
                    case EAssetState.SyncLoading:
                    case EAssetState.AsyncLoading:
                    case EAssetState.AsyncWaiting:
                        info.OnLoaded += o =>
                        {
                            if (typeof(T) == typeof(GameObject))
                                _asyncInstantiateWaitingList.Add(new AsyncInstantiateInfo(relPath, (Action<GameObject>) callback));
                            else
                                callback(o as T);
                        };
                        return;
                }
            }
            else
            {
                info = new AssetInfo(EAssetState.AsyncWaiting, isAtlas);
                info.OnLoaded += o =>
                {
                    if (typeof(T) == typeof(GameObject))
                        _asyncInstantiateWaitingList.Add(new AsyncInstantiateInfo(relPath, (Action<GameObject>) callback));
                    else
                        callback(o as T);
                };
                _assetDic.Add(relPath, info);
                
                // 增加资源引用
                info.AddRef();
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
                        
                            RecordABAsset(ab.name, relPath);

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
        
        /// <summary>
        /// 记录ab的资源
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="assetRelPath"></param>
        void RecordABAsset(string abName, string assetRelPath)
        {
            if (!_abAssetDic.TryGetValue(abName, out var set))
            {
                set = new HashSet<string>();
                _abAssetDic.Add(abName, set);
            }

            set.Add(assetRelPath);
        }

        /// <summary>
        /// 移除ab对应的资源
        /// </summary>
        /// <param name="abName"></param>
        void RemoveABAssets(string abName)
        {
            if (!_abAssetDic.TryGetValue(abName, out var set))
                return;

            foreach (var assetRelPath in set)
                _assetDic.Remove(assetRelPath);
        }

        void AddAtlasABRef(string atlasName)
        {
            var abRelPath = $"Atlas/{atlasName}";

            AddABRef(abRelPath);
        }

        void ReduceAtlasABRef(string atlasName)
        {
            var abRelPath = $"Atlas/{atlasName}";

            ReduceABRef(abRelPath);
        }

        T TryGetSpriteFromAtlas<T>(string atlasName, string spriteName, string relPath) where T: Object
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
                
                RecordABAsset(ab.name, relPath);
            }

            return asset;
        }
        
        void TryGetSpriteFromAtlasAsync<T>(string atlasName, string spriteName, AssetInfo info) where T: Object
        {
            T asset = null;

            spriteName = Path.GetFileNameWithoutExtension(spriteName);

            var abRelPath = $"Atlas/{atlasName}";

            if (_resLoadMode == EResLoadMode.AB || _resLoadMode == EResLoadMode.PackageAB)
            {
                // 加载ab
                LoadABAsync(abRelPath, ab =>
                {
                    // 加载资源
                    ab.LoadAssetAsync<SpriteAtlas>(atlasName);
                });
            }
            else
            {
                asset = TryGetSpriteFromAtlas<T>(atlasName, spriteName, abRelPath);
                
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