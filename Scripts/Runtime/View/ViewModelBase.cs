﻿using System;
using Engine.Scripts.Runtime.Event;

namespace Engine.Scripts.Runtime.View
{
    public abstract class ViewModelBase : IViewModel
    {
        public EventGroup EventGroup { get; private set; }

        public ViewModelBase()
        {
            EventGroup = new EventGroup(EEventGroup.GameLogic);
        }

        public abstract void Init();
        
        protected abstract void OnRegGameEvents();

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void Reg<T>(Action<T> callback) where T : EventDataBase
        {
            EventGroup.Reg(callback);
        }
        
        /// <summary>
        /// 同步广播
        /// </summary>
        /// <param name="data"></param>
        public void Broadcast(EventDataBase data)
        {
            EventGroup.Broadcast(data);
        }

        /// <summary>
        /// 异步同步广播
        /// </summary>
        /// <param name="data"></param>
        public void BroadcastAsync(EventDataBase data)
        {
            EventGroup.BroadcastAsync(data);
        }
    }
}