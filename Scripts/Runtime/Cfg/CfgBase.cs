using System;
using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Utils;
using UnityEngine;

namespace Engine.Scripts.Runtime.Cfg
{
    public abstract class CfgBase<T, K, C> : SingletonClass<T> where T : class, new() where C : CfgCellBase<K>
    {
        public int Count => _dataList.Count;
        
        private Dictionary<K, C> _dataDic = new Dictionary<K, C>();
        private List<C> _dataList = new List<C>();

        private LogGroup _log;
        
        public CfgBase()
        {
            _log = new LogGroup($"Cfg {GetType()}");
            
            var dataArr = OnGetDataArr();
            foreach (var data in dataArr)
            {
                _dataDic.Add(data.Id, data);
                _dataList.Add(data);
            }
        }

        protected abstract C[] OnGetDataArr();
        
        public C GetById(K key)
        {
            if (!_dataDic.TryGetValue(key, out var data))
            {
                _log.Error($"Can not find key '{key}' in cfg '{GetType()}'");
                
                return null;
            }
            
            return data;
        }

        public bool Has(K key)
        {
            return _dataDic.ContainsKey(key);
        }

        public bool TryGetById(K key, out C data)
        {
            return _dataDic.TryGetValue(key, out data);
        }

        public List<C> GetListCopy()
        {
            return new List<C>(_dataList);
        }

        /// <summary>
        /// 遍历配置表
        /// 返回true继续遍历，false中断遍历
        /// </summary>
        /// <param name="callback"></param>
        public void Foreach(Func<C, bool> callback)
        {
            if (callback == null)
                return;
            
            foreach (var val in _dataList)
                if (!callback(val))
                    break;
        }

        public List<C> Filter(Func<C, bool> checkHandler)
        {
            List<C> list = new List<C>();

            if (checkHandler == null)
                return list;
            
            Foreach(data =>
            {
                if (checkHandler(data))
                    list.Add(data);

                return true;
            });

            return list;
        }
    }
}