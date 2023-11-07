﻿using System;
using System.Collections.Generic;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Utils;
using UnityEngine;

namespace Engine.Scripts.Runtime.Timer
{
    public class TimerMgr : SingletonClass<TimerMgr>, IManager
    {
        // 计时器计次，用于自增作为id
        private static int _timerCnt = 0;
        
        private Dictionary<int, TimerInfo> _timerDic = new Dictionary<int, TimerInfo>();
        
        List<int> _removeList = new List<int>();
        List<TimerInfo> _callList = new List<TimerInfo>();
        
        public void Reset()
        {
            Clear();
        }

        public void Init()
        {
        }

        /// <summary>
        /// 用于外部计时
        /// </summary>
        /// <param name="delta"></param>
        public void OnTick(float delta)
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
                _timerCnt = 0;
            else
                _timerCnt++;
            
            return _timerCnt;
        }
    }
}