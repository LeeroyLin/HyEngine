﻿using System;
using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Timer;
using Engine.Scripts.Runtime.Utils;

namespace Engine.Scripts.Runtime.Event
{
    public abstract class EventDataBase
    {
    }

    public class EventMgr : SingletonClass<EventMgr>, IManager
    {
        private Dictionary<EEventGroup, Dictionary<string, List<int>>> _eventDic;
        private Dictionary<int, HandlerInfo> _cbDic;
        // 在下一帧再调用
        private List<AsyncInfo> _asyncList;

        private LogGroup _log;
        
        public void Reset()
        {
        }

        public void Init()
        {
            _eventDic = new();
            _cbDic = new();
            _asyncList = new();
            
            _log = new LogGroup("EventMgr");
            
            // 使用计时器
            TimerMgr.Ins.UseLoopTimer(0, OnTimer);
        }
        
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="group"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void Reg<T>(EEventGroup group, Action<T> callback) where T : EventDataBase
        {
            var groupDic = GetGroupDic(group);

            var key = GetKey<T>();

            if (!groupDic.TryGetValue(key, out var list))
            {
                list = new List<int>();
                groupDic.Add(key, list);
            }

            var handlerInfo = GetHandlerInfo(callback);

            list.Add(handlerInfo.Key);
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <param name="group"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void UnReg<T>(EEventGroup group, Action<T> callback) where T : EventDataBase
        {
            var groupDic = GetGroupDic(group);

            var key = GetKey<T>();

            if (!groupDic.TryGetValue(key, out var list))
            {
                _log.Warning("[UnReg] Can not find event. {0}", key);

                return;
            }
            
            var cbId = callback.GetHashCode();

            bool isOk = list.Remove(cbId);
            if (!isOk)
                _log.Warning("[UnReg] Can not find current event. {0}", key);

            if (_cbDic.TryGetValue(cbId, out var info))
            {
                info.ReduceRefCnt();

                if (info.RefCnt <= 0)
                    _cbDic.Remove(cbId);
            }
        }

        /// <summary>
        /// 同步广播
        /// </summary>
        /// <param name="group"></param>
        /// <param name="data"></param>
        public void Broadcast(EEventGroup group, EventDataBase data)
        {
            var key = GetKey(data);
            
            var groupDic = GetGroupDic(group);
            if (!groupDic.TryGetValue(key, out var list))
            {
                _log.Warning("[Broadcast] Can not find event. {0}", key);
                
                return;
            }

            foreach (var cbId in list)
            {
                if (_cbDic.TryGetValue(cbId, out var info))
                    info.Callback?.Invoke(data);
            }
        }

        /// <summary>
        /// 异步广播
        /// </summary>
        /// <param name="group"></param>
        /// <param name="data"></param>
        public void BroadcastAsync(EEventGroup group, EventDataBase data)
        {
            var key = GetKey(data);

            _asyncList.Add(new AsyncInfo(group, key, data));
        }

        /// <summary>
        /// 清除指定组的事件
        /// </summary>
        /// <param name="group"></param>
        public void ClearGroup(EEventGroup group)
        {
            _eventDic.Remove(group);
        }

        /// <summary>
        /// 清除对应事件
        /// </summary>
        /// <param name="group"></param>
        /// <typeparam name="T"></typeparam>
        public void ClearEvent<T>(EEventGroup group) where T : EventDataBase
        {
            if (_eventDic.TryGetValue(group, out var dic))
            {
                var key = GetKey<T>();

                dic.Remove(key);
            }
        }

        /// <summary>
        /// 清除对应事件
        /// </summary>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="cbId"></param>
        public void ClearEvent(EEventGroup group, string key, int cbId)
        {
            if (_eventDic.TryGetValue(group, out var dic))
            {
                if (dic.TryGetValue(key, out var list))
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i] == cbId)
                        {
                            list.RemoveAt(i);

                            if (_cbDic.TryGetValue(cbId, out var info))
                                info.ReduceRefCnt();
                            
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 清除所有事件
        /// </summary>
        public void ClearAll()
        {
            _eventDic.Clear();
        }

        void OnTimer()
        {
            foreach (var info in _asyncList)
            {
                var groupDic = GetGroupDic(info.Group);
                if (groupDic.TryGetValue(info.Key, out var list))
                {
                    foreach (var cbId in list)
                    {
                        if (_cbDic.TryGetValue(cbId, out var i))
                            i.Callback?.Invoke(info.Data);
                    }
                }
            }
            
            _asyncList.Clear();
        }

        // 获得组字典，没有则创建
        Dictionary<string, List<int>> GetGroupDic(EEventGroup group)
        {
            if (!_eventDic.TryGetValue(group, out var dic))
            {
                dic = new Dictionary<string, List<int>>();
                _eventDic.Add(group, dic);
            }

            return dic;
        }
        
        // 获得事件信息
        HandlerInfo GetHandlerInfo<T>(Action<T> callback) where T : EventDataBase
        {
            var cbId = callback.GetHashCode();
            
            if (!_cbDic.TryGetValue(cbId, out var info))
            {
                void newCallback(EventDataBase evt)
                {
                    callback(evt as T);
                }
                
                info = new HandlerInfo(cbId, newCallback);
                _cbDic.Add(cbId, info);
            }
            
            info.AddRefCnt();

            return info;
        }

        // 获得键
        private string GetKey(EventDataBase evt)
        {
            return evt.ToString();
        }
        private string GetKey<T>() where T : EventDataBase
        {
            return typeof(T).ToString();
        }
    }
}