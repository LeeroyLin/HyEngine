using System;
using System.Collections.Generic;

namespace Engine.Scripts.Runtime.ClassPool
{
    public class ClassPoolGroup<T, K> : IClassPoolGroup where T : class
    {
        public int Capacity { get; private set; }
        public int Count => _cache.Count;
        
        private Func<T> _creator;
        private Action<T> _resetHandler;
        private Action<T, K> _initHandler;
        private List<T> _cache;
        
        public ClassPoolGroup(Func<T> creator, Action<T, K> initHandler, Action<T> resetHandler, int capacity)
        {
            Capacity = capacity;
            _creator = creator;
            _initHandler = initHandler;
            _resetHandler = resetHandler;

            _cache = new List<T>();
        }

        public T GetIns(K userData)
        {
            T ins = null;

            if (Count == 0)
                ins = _creator?.Invoke();
            else
            {
                ins = _cache[Count - 1];
                _cache.RemoveAt(Count - 1);
            }
            
            _initHandler?.Invoke(ins, userData);

            return ins;
        }

        public void SetIns(T ins)
        {
            if (Count >= Capacity)
                return;

            _resetHandler?.Invoke(ins);
            
            _cache.Add(ins);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public void SetCapacity(int capacity)
        {
            Capacity = capacity;
        }

        public int GetCapacity()
        {
            return Capacity;
        }
    }
}