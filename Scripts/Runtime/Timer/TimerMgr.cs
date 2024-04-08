using System;
using System.Collections.Generic;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Utils;
using UnityEngine;
using UnityEngine.Profiling;

namespace Engine.Scripts.Runtime.Timer
{
    public class TimerMgr : SingletonClass<TimerMgr>, IManager
    {
        // 计时器计次，用于自增作为id
        private static int _timerCnt = 1;
        
        private Dictionary<int, TimerInfo> _timerDic = new Dictionary<int, TimerInfo>();
        
        List<int> _removeList = new List<int>();
        List<TimerInfo> _callList = new List<TimerInfo>();

        private Dictionary<int, Action> _updateDic = new Dictionary<int, Action>();
        private Dictionary<int, Action> _lateUpdateDic = new Dictionary<int, Action>();
        private Dictionary<int, Action> _fixedUpdateDic = new Dictionary<int, Action>();

        private List<Action> _actions = new List<Action>();
        
        public void Reset()
        {
            _timerCnt = 1;
            Clear();
        }

        public void Init()
        {
        }

        public void Dispose()
        {
            _timerDic.Clear();
            _removeList.Clear();
            _callList.Clear();
            _updateDic.Clear();
            _lateUpdateDic.Clear();
            _fixedUpdateDic.Clear();
        }

        public void OnUpdate()
        {
            _actions.Clear();
            
            foreach (var data in _updateDic)
                _actions.Add(data.Value);

            foreach (var action in _actions)
                action();
        }

        public void OnLateUpdate()
        {
            _actions.Clear();

            foreach (var data in _lateUpdateDic)
                _actions.Add(data.Value);

            foreach (var action in _actions)
            {
                Profiler.BeginSample(action.Target.ToString());
                action();
                Profiler.EndSample();
            }
        }

        public void OnFixedUpdate()
        {
            OnTick();
            
            _actions.Clear();

            foreach (var data in _fixedUpdateDic)
                _actions.Add(data.Value);

            foreach (var action in _actions)
                action();
        }

        public void UseUpdate(Action callback)
        {
            _updateDic.TryAdd(callback.GetHashCode(), callback);
        }

        public void RemoveUpdate(Action callback)
        {
            _updateDic.Remove(callback.GetHashCode());
        }

        public void UseLateUpdate(Action callback)
        {
            _lateUpdateDic.TryAdd(callback.GetHashCode(), callback);
        }

        public void RemoveLateUpdate(Action callback)
        {
            _lateUpdateDic.Remove(callback.GetHashCode());
        }

        public void UseFixedUpdate(Action callback)
        {
            _fixedUpdateDic.TryAdd(callback.GetHashCode(), callback);
        }

        public void RemoveFixedUpdate(Action callback)
        {
            _fixedUpdateDic.Remove(callback.GetHashCode());
        }

        /// <summary>
        /// 用于外部计时
        /// </summary>
        public void OnTick()
        {
            _removeList.Clear();
            _callList.Clear();

            foreach (var info in _timerDic)
            {
                // 是否该轮时间满足
                if (info.Value.IsTimeOK())
                {
                    // 标记回调
                    info.Value.MarkCall();
                    _callList.Add(info.Value);

                    // 是否计时器结束
                    if (info.Value.IsDead())
                        _removeList.Add(info.Key);
                }
            }
            
            // 移除
            foreach (var key in _removeList)
            {
                _timerDic.Remove(key);
            }
            
            // 回调
            foreach (var info in _callList)
            {
                info.Call();
            }
        }

        /// <summary>
        /// 使用一次性计时器
        /// </summary>
        /// <param name="delay">延迟时间，秒</param>
        /// <param name="callback">回调方法</param>
        /// <returns>计时器id</returns>
        public int UseOnceTimer(float delay, Action callback)
        {
            if (delay < 0)
                delay = 0;
            
            var id = GetNewTimerId();

            var timer = new TimerInfo(delay, callback);
            
            _timerDic.Add(id, timer);

            return id;
        }

        /// <summary>
        /// 使用循环计时器
        /// </summary>
        /// <param name="interval">调用间隔秒</param>
        /// <param name="callback">回调方法</param>
        /// <param name="delay">第一次调用延迟，默认与interval相同</param>
        /// <param name="limited">总调用次数限制</param>
        /// <returns></returns>
        public int UseLoopTimer(float interval, Action callback, float delay = -1, int limited = -1)
        {
            if (delay < 0)
                delay = interval;
            
            var id = GetNewTimerId();

            var timer = new TimerInfo(delay, interval, limited, callback);
            
            _timerDic.Add(id, timer);

            return id;
        }

        /// <summary>
        /// 根据计时器id移除计时器
        /// </summary>
        /// <param name="timerId">计时器</param>
        public void RemoveTimer(int timerId)
        {
            _timerDic.Remove(timerId);
        }
        
        /// <summary>
        /// 清空计时器
        /// </summary>
        public void Clear()
        {
            _timerDic.Clear();
        }

        // 获得新计时器Id
        private int GetNewTimerId()
        {
            if (_timerCnt == int.MaxValue)
                _timerCnt = 1;
            else
                _timerCnt++;
            
            return _timerCnt;
        }
    }
}