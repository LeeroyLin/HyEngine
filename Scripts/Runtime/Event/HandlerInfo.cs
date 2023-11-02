using System;
using UnityEngine;

namespace Engine.Scripts.Runtime.Event
{
    class HandlerInfo
    {
        public int Key { get; private set; }
        public Action<EventDataBase> Callback { get; private set; }
        public int RefCnt { get; private set; }

        public HandlerInfo(int key, Action<EventDataBase> callback)
        {
            Key = key;
            Callback = callback;
        } 
        
        public void AddRefCnt()
        {
            RefCnt++;
        }

        public void ReduceRefCnt()
        {
            RefCnt = Mathf.Max(0, --RefCnt);
        }
    }

}