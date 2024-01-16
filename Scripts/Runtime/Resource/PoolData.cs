using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
    public class PoolData
    {
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
        /// 添加新的
        /// </summary>
        /// <param name="obj"></param>
        public void Add(GameObject obj)
        {
            // 是否满了
            if (IsFull)
            {
                // 直接销毁
                _destroyHandler?.Invoke(obj);
                return;
            }

            // 是否没有节点
            if (Node == null)
            {
                // 创建节点
                Node = new GameObject(Key).transform;
                Node.name = Key;
                Node.SetParent(PoolRoot, false);
            }

            // 存储在节点下
            obj.transform.SetParent(Node, false);

            // 隐藏
            obj.SetActive(false);

            // 记录
            listTrans.Add(obj.transform);
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

            node.SetParent(null);

            var obj = node.gameObject;
            
            // 显示
            obj.SetActive(true);

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
                // 销毁节点
                _destroyHandler?.Invoke(Node.GetChild(i).gameObject);
            }

            // 重置数据
            listTrans.Clear();
            SetCapacity(PoolMgr.DEFAULT_CAPACITY);
        }
    }
}