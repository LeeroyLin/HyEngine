using System;
using Engine.Scripts.Runtime.Event;
using Engine.Scripts.Runtime.Utils;

namespace Engine.Scripts.Runtime.Model
{
    public abstract class ModelBase<T> : SingletonClass<T>, IModel where T : class, new()
    {
        protected EventGroup NetEventGroup { get; private set; }
        protected EventGroup GameEventGroup { get; private set; }
        
        public ModelBase()
        {
            NetEventGroup = new EventGroup(EEventGroup.Net);
            GameEventGroup = new EventGroup(EEventGroup.GameLogic);
            
            OnRegNetEvents();
            OnRegGameEvents();
            
            InitData();
        }
        
        public void Reset()
        {            
            NetEventGroup.ClearCurrentAllEvents();
            GameEventGroup.ClearCurrentAllEvents();
         
            OnReset();
   
            OnRegNetEvents();
            OnRegGameEvents();
        }

        protected abstract void InitData();
        protected abstract void OnRegNetEvents();
        protected abstract void OnRegGameEvents();
        protected abstract void OnReset();
        
        /// <summary>
        /// 注册网络事件
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void RegNet<K>(Action<K> callback) where K : IEventData
        {
            NetEventGroup.Reg(callback);
        }
        
        /// <summary>
        /// 同步广播网络事件
        /// </summary>
        /// <param name="data"></param>
        public void BroadcastNet(IEventData data)
        {
            NetEventGroup.Broadcast(data);
        }

        /// <summary>
        /// 异步同步广播网络事件
        /// </summary>
        /// <param name="data"></param>
        public void BroadcastNetAsync(IEventData data)
        {
            NetEventGroup.BroadcastAsync(data);
        }
        
        /// <summary>
        /// 注册游戏事件
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void RegGame<K>(Action<K> callback) where K : IEventData
        {
            GameEventGroup.Reg(callback);
        }
        
        /// <summary>
        /// 注册游戏事件
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void UnRegGame<K>(Action<K> callback) where K : IEventData
        {
            GameEventGroup.UnReg(callback);
        }
        
        /// <summary>
        /// 同步广播游戏事件
        /// </summary>
        /// <param name="data"></param>
        public void BroadcastGame(IEventData data)
        {
            GameEventGroup.Broadcast(data);
        }

        /// <summary>
        /// 异步同步广播游戏事件
        /// </summary>
        /// <param name="data"></param>
        public void BroadcastGameAsync(IEventData data)
        {
            GameEventGroup.BroadcastAsync(data);
        }
    }
}