using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
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
        /// 存储变换组件的列表
        /// </summary>
        public List<Transform> listTrans;

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
        /// 是否满了
        /// </summary>
        public bool IsFull
        {
            get { return listTrans == null || listTrans.Count >= Capacity; }
        }
        
        /// <summary>
        /// 是否需要隐藏，隐藏要修改父节点，active设置为false
        /// </summary>
        public bool IsNeedHide { get; private set; }

        private Action<GameObject> _destroyHandler;
        private Func<string, GameObject> _createHandler;
        private Func<string, Task<GameObject>> _createAsyncHandler;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="poolRoot">对象池根节点</param>
        /// <param name="destroyHandler">对象移除方法</param>
        /// <param name="createHandler">对象创建方法</param>
        /// <param name="createAsyncHandler">对象创建方法</param>
        public PoolData(string key, Transform poolRoot, Action<GameObject> destroyHandler, 
            Func<string, GameObject> createHandler, Func<string, Task<GameObject>> createAsyncHandler)
        {
            _destroyHandler = destroyHandler;
            _createHandler = createHandler;
            _createAsyncHandler = createAsyncHandler;
            PoolRoot = poolRoot;
            
            Key = key;
            listTrans = new List<Transform>();
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
            listTrans.Add(trans);
        }

        /// <summary>
        /// 尝试获取
        /// </summary>
        /// <returns></returns>
        public GameObject TryGet()
        {
            if (listTrans.Count == 0)
                return _createHandler?.Invoke(Key);

            // 取出最后一个
            Transform node = listTrans[listTrans.Count - 1];

            // 去除数据
            listTrans.RemoveAt(listTrans.Count - 1);

            var obj = node.gameObject;

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
            if (listTrans.Count == 0)
                return await _createAsyncHandler(Key);

            // 取出最后一个
            Transform node = listTrans[listTrans.Count - 1];

            // 去除数据
            listTrans.RemoveAt(listTrans.Count - 1);

            node.SetParent(null);

            var obj = node.gameObject;
            
            // 显示
            obj.SetActive(true);

            return obj;
        }

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            if (listTrans.Count == 0)
            {
                return;
            }

            // 反向遍历节点
            for (int i = Node.childCount; i >= 0; i--)
            {
                var trans = Node.GetChild(i);
                trans.SetParent(null);
                
                // 销毁节点
                _destroyHandler?.Invoke(trans.gameObject);
            }

            // 重置数据
            listTrans.Clear();
            SetCapacity(PoolMgr.DEFAULT_CAPACITY);
        }
    }
}