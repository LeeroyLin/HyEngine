using System;
using UnityEngine;

namespace Engine.Scripts.Runtime.Event
{
    class HandlerInfo
    {
        public int Key { get; private set; }
        public Action<IEventData> Callback { get; private set; }
        public int RefCnt { get; private set; }

        public HandlerInfo(int key, Action<IEventData> callback)
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