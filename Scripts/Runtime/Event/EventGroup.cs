using System;
using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;

namespace Engine.Scripts.Runtime.Event
{
    public class EventGroup
    {
        public int Group { get; private set; }

        private Dictionary<int, int> _handlerDic;

        private LogGroup _log;

        public EventGroup(int group)
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
        public void Reg<T>(Action<T> callback) where T : EventDataBase
        {
            var cbId = callback.GetHashCode();
            
            // 是否已经存在
            if (_handlerDic.ContainsKey(cbId))
            {
                _log.Warning("[Reg] Can not register same callback.");
                
                return;
            }
            
            _handlerDic.Add(cbId, cbId);
            EventMgr.Ins.Reg(Group, callback);
        }
        
        /// <summary>
        /// 取消注册
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void UnReg<T>(Action<T> callback) where T : EventDataBase
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
        public void Broadcast(EventDataBase data)
        {
            EventMgr.Ins.Broadcast(Group, data);
        }

        /// <summary>
        /// 异步同步广播
        /// </summary>
        /// <param name="data"></param>
        public void BroadcastAsync(EventDataBase data)
        {
            EventMgr.Ins.BroadcastAsync(Group, data);
        }

        /// <summary>
        /// 清除组事件
        /// </summary>
        public void ClearGroup()
        {
            EventMgr.Ins.ClearGroup(Group);
        }

        /// <summary>
        /// 清除对应事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ClearEvent<T>() where T : EventDataBase
        {
            EventMgr.Ins.ClearEvent<T>(Group);
        }
    }
}