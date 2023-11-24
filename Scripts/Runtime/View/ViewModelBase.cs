using System;
using Engine.Scripts.Runtime.Event;
using UnityEngine;

namespace Engine.Scripts.Runtime.View
{
    public abstract class ViewModelBase : IViewModel
    {
        public EventGroup EventGroup { get; private set; }

        public ViewArgsBase Args { get; private set; }

        public ViewModelBase()
        {
            EventGroup = new EventGroup(EEventGroup.GameLogic);
        }

        public void Init(ViewBase view, ViewArgsBase args)
        {
            Args = args;
         
            OnRegGameEvents();
            
            OnInit(view);
        }
        
        public abstract void OnInit(ViewBase view);
        
        protected abstract void OnRegGameEvents();

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void Reg<T>(Action<T> callback) where T : IEventData
        {
            EventGroup.Reg(callback);
        }
        
        /// <summary>
        /// 同步广播
        /// </summary>
        /// <param name="data"></param>
        public void Broadcast(IEventData data)
        {
            EventGroup.Broadcast(data);
        }

        /// <summary>
        /// 异步同步广播
        /// </summary>
        /// <param name="data"></param>
        public void BroadcastAsync(IEventData data)
        {
            EventGroup.BroadcastAsync(data);
        }
    }
}