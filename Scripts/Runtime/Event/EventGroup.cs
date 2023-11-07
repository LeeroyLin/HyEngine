using System;
using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;

namespace Engine.Scripts.Runtime.Event
{
    public class EventGroup
    {
        public EEventGroup Group { get; private set; }

        private Dictionary<int, string> _handlerDic;

        private LogGroup _log;

        public EventGroup(EEventGroup group)
        {
            Group = group;
            _handlerDic = new();
            _log = new LogGroup("EventGroup");
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void Reg<T>(Action<T> callback) where T : IEventData
        {
            var key = typeof(T).ToString();
            var cbId = callback.GetHashCode();
            
            // 是否已经存在
            if (_handlerDic.ContainsKey(cbId))
            {
                _log.Warning("[Reg] Can not register same callback.");
                
                return;
            }
            
            _handlerDic.Add(cbId, key);
            EventMgr.Ins.Reg(Group, callback);
        }
        
        /// <summary>
        /// 取消注册
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void UnReg<T>(Action<T> callback) where T : IEventData
        {
            var cbId = callback.GetHashCode();

            if (!_handlerDic.ContainsKey(cbId))
            {
                _log.Warning("[UnReg] There is no this callback.");
                
                return;
            }
            
            _handlerDic.Remove(cbId);
            EventMgr.Ins.UnReg(Group, callback);
        }

        /// <summary>
        /// 同步广播
        /// </summary>
        /// <param name="data"></param>
        public void Broadcast(IEventData data)
        {
            EventMgr.Ins.Broadcast(Group, data);
        }

        /// <summary>
        /// 异步同步广播
        /// </summary>
        /// <param name="data"></param>
        public void BroadcastAsync(IEventData data)
        {
            EventMgr.Ins.BroadcastAsync(Group, data);
        }

        /// <summary>
        /// 移除通过该对象注册的所有事件
        /// </summary>
        public void ClearCurrentAllEvents()
        {
            foreach (var info in _handlerDic)
            {
                EventMgr.Ins.ClearEvent(Group, info.Value, info.Key);
            }
        }
    }
}