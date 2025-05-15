using System;
using Engine.Scripts.Runtime.Event;

namespace Engine.Scripts.Runtime.System
{
    public abstract class SystemBase<T> : ISystem where T : SystemModelBase, new()
    {
        public T SystemModel { get; private set; }
        public EventGroup EventGroup { get; private set; }
        
        public bool IsSystemExited { get; private set; }

        public SystemBase()
        {
            EventGroup = new EventGroup(EEventGroup.GameLogic);
            
            SystemModel = new T();
            
            SystemModel.Init();
        }
        
        public void Enter()
        {
            IsSystemExited = false;
            
            OnRegGameEvents();
            
            OnEnter();
        }

        public void Exit()
        {
            IsSystemExited = true;
            
            EventGroup.ClearCurrentAllEvents();
            
            OnExit();
        }

        protected abstract void OnEnter();

        protected abstract void OnExit();
        
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