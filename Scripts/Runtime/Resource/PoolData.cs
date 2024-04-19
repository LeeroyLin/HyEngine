using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
    struct CacheInfo
    {
        public Transform trans;
        public float cacheAt;

        public CacheInfo(Transform trans)
        {
            this.trans = trans;
            cacheAt = Time.time;
        }
    }
    
    public class PoolData
    {
        /// <summary>
        /// 存放时的坐标
        /// </summary>
        private static readonly Vector3 CACHE_WPOS = new Vector3(-10000, -10000, 0);
        
        /// <summary>
        /// 键名
        /// </summary>
        public string Key;

        /// <summary>
        /// 存储节点
        /// </summary>
        public Transform Node { get; private set; }

        /// <summary>
        /// 存储节点
        /// </summary>
        public Transform PoolRoot { get; private set; }

        /// <summary>
        /// 最大容量
        /// 超过该值后则直接销毁
        /// </summary>
        public int Capacity { get; private set; }
        
        /// <summary>
        /// 缓存最大时间，缓存超过这个时间则销毁
        /// </summary>
        public float CacheMaxTime { get; private set; }
        
        /// <summary>
        /// 最少缓存数，低于这个缓存值不会自动随时间销毁
        /// </summary>
        public int MinCacheNum { get; private set; }

        /// <summary>
        /// 是否满了
        /// </summary>
        public bool IsFull
        {
            get { return _listCache == null || _listCache.Count >= Capacity; }
        }
        
        /// <summary>
        /// 是否需要隐藏，隐藏要修改父节点，active设置为false
        /// </summary>
        public bool IsNeedHide { get; private set; }

        private Action<GameObject> _destroyHandler;
        private Func<string, GameObject> _createHandler;
        private Func<string, Task<GameObject>> _createAsyncHandler;

        /// <summary>
        /// 存储缓存信息的列表
        /// </summary>
        List<CacheInfo> _listCache;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="cacheMaxTime">缓存最大时间，缓存超过这个时间则销毁</param>
        /// <param name="minCacheNum">最少缓存数，低于这个缓存值不会自动随时间销毁</param>
        /// <param name="poolRoot">对象池根节点</param>
        /// <param name="destroyHandler">对象移除方法</param>
        /// <param name="createHandler">对象创建方法</param>
        /// <param name="createAsyncHandler">对象创建方法</param>
        public PoolData(string key, float cacheMaxTime, int minCacheNum, Transform poolRoot, Action<GameObject> destroyHandler, 
            Func<string, GameObject> createHandler, Func<string, Task<GameObject>> createAsyncHandler)
        {
            _destroyHandler = destroyHandler;
            _createHandler = createHandler;
            _createAsyncHandler = createAsyncHandler;
            PoolRoot = poolRoot;
            
            Key = key;
            CacheMaxTime = cacheMaxTime;
            MinCacheNum = minCacheNum;
            _listCache = new List<CacheInfo>();
            Capacity = PoolMgr.DEFAULT_CAPACITY;
            IsNeedHide = true;
        }

        /// <summary>
        /// 设置容量
        /// </summary>
        /// <param name="capacity"></param>
        public void SetCapacity(int capacity)
        {
            Capacity = capacity;
        }

        /// <summary>
        /// 设置缓存最大时间，缓存超过这个时间则销毁
        /// </summary>
        public void SetCacheMaxTime(float cacheMaxTime)
        {
            CacheMaxTime = cacheMaxTime;
        }

        /// <summary>
        /// 设置最少缓存数，低于这个缓存值不会自动随时间销毁
        /// </summary>
        public void SetMinCacheNum(int minCacheNum)
        {
            MinCacheNum = minCacheNum;
        }

        /// <summary>
        /// 设置回收时是否需要改变节点和隐藏
        /// </summary>
        public void SetNeedHide(bool isNeedHide)
        {
            IsNeedHide = isNeedHide;
        }

        /// <summary>
        /// 添加新的
        /// </summary>
        /// <param name="obj"></param>
        public void Add(GameObject obj)
        {
            var trans = obj.transform;
            
            // 是否满了
            if (IsFull)
            {
                trans.SetParent(null);
                
                // 直接销毁
                _destroyHandler?.Invoke(obj);
                return;
            }

            if (IsNeedHide)
            {
                // 是否没有节点
                if (Node == null)
                {
                    // 创建节点
                    Node = new GameObject(Key).transform;
                    Node.name = Key;
                    Node.SetParent(PoolRoot, false);
                }

                // 存储在节点下
                trans.SetParent(Node, false);

                // 隐藏
                obj.SetActive(false);
            }
            else
            {
                trans.position = CACHE_WPOS;
            }

            // 记录
            _listCache.Add(new CacheInfo(trans));
        }

        /// <summary>
        /// 尝试获取
        /// </summary>
        /// <returns></returns>
        public GameObject TryGet()
        {
            if (_listCache.Count == 0)
                return _createHandler?.Invoke(Key);

            // 取出最后一个
            var info = _listCache[_listCache.Count - 1];

            // 去除数据
            _listCache.RemoveAt(_listCache.Count - 1);

            var obj = info.trans.gameObject;

            if (IsNeedHide)
            {
                // 显示
                obj.SetActive(true);
            }

            return obj;
        }

        /// <summary>
        /// 尝试异步获取
        /// </summary>
        /// <returns></returns>
        public async Task<GameObject> TryGetAsync()
        {
            if (_listCache.Count == 0)
                return await _createAsyncHandler(Key);

            // 取出最后一个
            var info = _listCache[_listCache.Count - 1];

            // 去除数据
            _listCache.RemoveAt(_listCache.Count - 1);

            info.trans.SetParent(null);

            var obj = info.trans.gameObject;
            
            // 显示
            obj.SetActive(true);

            return obj;
        }

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            if (_listCache.Count == 0)
                return;

            for (int i = _listCache.Count - 1; i >= 0; i--)
            {
                var info = _listCache[i];
                
                // 销毁节点
                _destroyHandler?.Invoke(info.trans.gameObject);
            }

            // 重置数据
            _listCache.Clear();
            SetCapacity(PoolMgr.DEFAULT_CAPACITY);
        }

        public void Tick(float dt)
        {
            if (_listCache.Count <= MinCacheNum)
                return;

            int canDestroyNum = _listCache.Count - MinCacheNum;
            int cnt = 0;

            for (int i = _listCache.Count - 1; i >= 0; i--)
            {
                var info = _listCache[i];

                // 超时
                if (Time.time - info.cacheAt >= CacheMaxTime)
                {
                    // 销毁节点
                    _destroyHandler?.Invoke(info.trans.gameObject);
                    
                    // 移除数据
                    _listCache.RemoveAt(i);
                    
                    cnt++;

                    if (cnt >= canDestroyNum)
                        break;
                }
            }
        }
    }
}