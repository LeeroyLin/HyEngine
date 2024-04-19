using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Timer;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Scripts.Runtime.Resource
{
    public class PoolMgr : ManagerBase<PoolMgr>
    {
        /// <summary>
        /// 默认容量
        /// </summary>
        public static readonly int DEFAULT_CAPACITY = 10;
        
        /// <summary>
        /// 默认 缓存最大时间，超过该时间则会自动销毁缓存对象
        /// </summary>
        public static readonly float DEFAULT_CACHE_MAX_TIME = 20;
        
        /// <summary>
        /// 默认 最小缓存数，缓存个数小于该值不会自动销毁
        /// </summary>
        public static readonly int DEFAULT_MIN_CACHE_NUM = 0;

        /// <summary>
        /// 对象池根节点
        /// </summary>
        public Transform PoolRoot { get; private set; }
        
        /// <summary>
        /// 对象池字典
        /// 键为资源Key
        /// </summary>
        Dictionary<string, PoolData> _dicPool = new Dictionary<string, PoolData>();

        private Action<GameObject> _destroyHandler;
        private Func<string, GameObject> _createHandler;
        private Func<string, Task<GameObject>> _createAsyncHandler;
        
        public void Init(Func<string, GameObject> createHandler, Func<string, Task<GameObject>> createAsyncHandler, Action<GameObject> destroyHandler)
        {
            _createHandler = createHandler;
            _createAsyncHandler = createAsyncHandler;
            _destroyHandler = destroyHandler;
            
            CreateNode();
            
            TimerMgr.Ins.UseLateUpdate(OnLateUpdate);
        }

        protected override void OnReset()
        {
            ClearAll();
            
            TimerMgr.Ins.RemoveLateUpdate(OnLateUpdate);
            TimerMgr.Ins.UseLateUpdate(OnLateUpdate);
        }

        protected override void OnDisposed()
        {
            ClearAll();
            
            RemoveNode();
            
            TimerMgr.Ins.RemoveLateUpdate(OnLateUpdate);
        }

        /// <summary>
        /// 回收
        /// </summary>
        /// <param name="obj">游戏对象</param>
        /// <param name="customKey">自定义键</param>
        public void Set(GameObject obj, string customKey = null)
        {
            if (IsDisposed)
            {
                // 直接销毁
                _destroyHandler?.Invoke(obj);
                return;
            }
            
            // 获得资源数据
            AssetData data = obj.GetComponent<AssetData>();

            // 如果没有资源数据
            if (data == null)
            {
                obj.transform.SetParent(null);
                
                // 直接销毁
                _destroyHandler?.Invoke(obj);
                return;
            }

            // 获得键
            string key = customKey;
            if (string.IsNullOrEmpty(key))
                key = data.relPath;

            // 是否没有数据
            if (!_dicPool.TryGetValue(key, out PoolData poolData))
            {
                // 创建数据
                poolData = new PoolData(key, DEFAULT_CACHE_MAX_TIME, DEFAULT_MIN_CACHE_NUM, PoolRoot, _destroyHandler, _createHandler, _createAsyncHandler);
                _dicPool.Add(key, poolData);
            }

            // 添加
            poolData.Add(obj);
        }

        /// <summary>
        /// 尝试获取
        /// </summary>
        /// <param name="key">键名</param>
        /// <returns></returns>
        public GameObject Get(string key)
        {
            // 是否有数据
            if (_dicPool.TryGetValue(key, out PoolData poolData))
                return poolData.TryGet();
            
            return _createHandler?.Invoke(key);
        }

        /// <summary>
        /// 尝试异步获取
        /// </summary>
        /// <param name="key"></param>
        /// <param name="finished"></param>
        /// <returns></returns>
        public async Task<GameObject> GetAsync(string key, Action<GameObject> finished = null)
        {
            // 是否有数据
            if (_dicPool.TryGetValue(key, out PoolData poolData))
            {
                var o = await poolData.TryGetAsync();
                finished?.Invoke(o);
                return o;
            }
            
            var obj = await _createAsyncHandler(key);
            
            finished?.Invoke(obj);
            
            return obj;
        }

        /// <summary>
        /// 设置容量
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="capacity">容量</param>
        public void SetCapacity(string key, int capacity)
        {
            // 是否没有数据
            if (!_dicPool.TryGetValue(key, out PoolData poolData))
            {
                poolData = new PoolData(key, DEFAULT_CACHE_MAX_TIME, DEFAULT_MIN_CACHE_NUM, PoolRoot, _destroyHandler, _createHandler, _createAsyncHandler);
                _dicPool.Add(key, poolData);
            }
            
            poolData.SetCapacity(capacity);
        }

        /// <summary>
        /// 设置回收时是否需要改变节点和隐藏
        /// <param name="key">键名</param>
        /// </summary>
        public void SetNeedHide(string key, bool isNeedHide)
        {
            // 是否没有数据
            if (!_dicPool.TryGetValue(key, out PoolData poolData))
            {
                poolData = new PoolData(key, DEFAULT_CACHE_MAX_TIME, DEFAULT_MIN_CACHE_NUM, PoolRoot, _destroyHandler, _createHandler, _createAsyncHandler);
                _dicPool.Add(key, poolData);
            }
            
            poolData.SetNeedHide(isNeedHide);
        }

        /// <summary>
        /// 设置缓存最大时间，缓存超过这个时间则销毁
        /// </summary>
        public void SetCacheMaxTime(string key, float cacheMaxTime)
        {
            // 是否没有数据
            if (!_dicPool.TryGetValue(key, out PoolData poolData))
            {
                poolData = new PoolData(key, DEFAULT_CACHE_MAX_TIME, DEFAULT_MIN_CACHE_NUM, PoolRoot, _destroyHandler, _createHandler, _createAsyncHandler);
                _dicPool.Add(key, poolData);
            }

            poolData.SetCacheMaxTime(cacheMaxTime);
        }

        /// <summary>
        /// 设置最少缓存数，低于这个缓存值不会自动随时间销毁
        /// </summary>
        public void SetMinCacheNum(string key, int minCacheNum)
        {
            // 是否没有数据
            if (!_dicPool.TryGetValue(key, out PoolData poolData))
            {
                poolData = new PoolData(key, DEFAULT_CACHE_MAX_TIME, DEFAULT_MIN_CACHE_NUM, PoolRoot, _destroyHandler, _createHandler, _createAsyncHandler);
                _dicPool.Add(key, poolData);
            }

            poolData.SetMinCacheNum(minCacheNum);
        }
        
        /// <summary>
        /// 清除指定键名的对象池
        /// </summary>
        /// <param name="key">键名</param>
        public void Clear(string key)
        {
            // 是否有数据
            if (_dicPool.TryGetValue(key, out PoolData poolData))
            {
                // 清除数据
                poolData.Clear();

                // 移除数据
                _dicPool.Remove(key);
            }
        }

        /// <summary>
        /// 清除所有对象池
        /// </summary>
        public void ClearAll()
        {
            // 遍历
            foreach (KeyValuePair<string, PoolData> item in _dicPool)
            {
                // 清除
                item.Value.Clear();
            }

            // 清除数据
            _dicPool.Clear();
        }

        void CreateNode()
        {
            if (PoolRoot == null)
                PoolRoot = new GameObject("PoolRoot").transform;
        }

        void RemoveNode()
        {
            if (PoolRoot != null)
            {
                Object.Destroy(PoolRoot.gameObject);
                PoolRoot = null;
            }
        }

        void OnLateUpdate()
        {
            foreach (var kv in _dicPool)
                kv.Value.Tick(Time.deltaTime);
        }
    }
}