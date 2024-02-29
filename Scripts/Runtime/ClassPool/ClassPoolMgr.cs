using System;
using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Utils;

namespace Engine.Scripts.Runtime.ClassPool
{
    public class ClassPoolMgr : SingletonClass<ClassPoolMgr>, IManager
    {
        private Dictionary<string, IClassPoolGroup> _groupDic = new Dictionary<string, IClassPoolGroup>();

        private LogGroup _log;
        
        public void Reset()
        {
        }
        
        public void Init()
        {
            _log = new LogGroup("ClassPoolMgr");
        }

        public void InitGroup<T, K>(Func<T> creator, Action<T, K> initHandler, Action<T> resetHandler, int capacity) where T : class
        {
            string key = typeof(T).FullName;

            if (string.IsNullOrEmpty(key))
            {
                _log.Error("Can not init group with empty name");
                
                return;
            }

            if (!_groupDic.TryGetValue(key, out var group))
            {
                group = new ClassPoolGroup<T, K>(creator, initHandler, resetHandler, capacity);
                _groupDic.Add(key, group);
            }
            else
            {
                if (capacity > group.GetCapacity())
                    group.SetCapacity(capacity);
            }
        }

        public T GetIns<T, K>(K userData) where T : class
        {
            string key = typeof(T).FullName;

            if (!_groupDic.TryGetValue(key, out var group))
            {
                _log.Error("GetIns. Can not get group with empty name");
                
                return null;
            }

            return ((ClassPoolGroup<T, K>)group).GetIns(userData);
        }

        public void SetIns<T, K>(T ins) where T : class
        {
            if (ins == null)
                return;
            
            string key = typeof(T).FullName;

            if (!_groupDic.TryGetValue(key, out var group))
            {
                _log.Error("SetIns. Can not get group with empty name");
                
                return;
            }

            ((ClassPoolGroup<T, K>)group).SetIns(ins);
        }

        public void Clear<T, K>() where T : class
        {
            string key = typeof(T).FullName;

            if (!_groupDic.TryGetValue(key, out var group))
            {
                _log.Error("Clear. Can not get group with empty name");
                
                return;
            }
            
            ((ClassPoolGroup<T, K>)group).Clear();
        }
        
        public void ClearAll()
        {
            foreach (var kv in _groupDic)
                kv.Value.Clear();    
            
            _groupDic.Clear();
        }
    }
}