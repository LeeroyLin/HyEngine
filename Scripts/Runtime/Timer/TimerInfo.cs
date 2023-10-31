using System;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Utils;

namespace Engine.Scripts.Runtime.Timer
{
    public class TimerInfo
    {
        // 距离第一次执行的时间 秒
        public float Delay { get; }

        // 后续循环执行的间隔 秒
        public float Interval { get; }
        
        // 总调用次数限制
        public int CntLimited { get; }
        
        // 回调
        private Action _callback;
        
        // 每轮开始时间
        private long _startAtMS;

        // 总计次
        private int _cnt;
        
        // 是否标记回调
        private bool _isMarked;

        public TimerInfo(float delay, Action callback)
        {
            _startAtMS = TimeUtil.GetTimestampMS();
            _cnt = 0;
            
            Delay = delay;
            Interval = 0;
            CntLimited = 1;
            _callback = callback;
        }
        
        public TimerInfo(float delay, float interval, int limited, Action callback)
        {
            _startAtMS = TimeUtil.GetTimestampMS();
            _cnt = 0;
            
            Delay = delay;
            Interval = interval;
            CntLimited = limited;
            _callback = callback;
        }

        /// <summary>
        /// 执行回调方法
        /// 回调前必须先标记
        /// </summary>
        public void Call()
        {
            if (!_isMarked)
            {
                LogMgr.Ins.LogError("【TimerInfo】[Call] Can not call before mark.");
                
                return;
            }

            _isMarked = false;
            
            _callback?.Invoke();
        }
        
        /// <summary>
        /// 是否时间满足了
        /// </summary>
        /// <param name="nowMS">当前毫秒级时间戳</param>
        /// <returns></returns>
        public bool IsTimeOK()
        {
            var targetTime = _cnt == 0 ? Delay : Interval;
            long targetTimeMS = (long)(targetTime * 1000);
            var leftMS = TimeUtil.LeftMS(_startAtMS + targetTimeMS);
            return leftMS <= 0;
        }

        /// <summary>
        /// 标记即将回调
        /// 外部循环内先标记，后续统一回调，避免外部回调方法中操作计时器
        /// </summary>
        public void MarkCall()
        {
            if (_isMarked)
                return;
            
            _cnt++;
            _isMarked = true;
            
            _startAtMS = TimeUtil.GetTimestampMS();
        }

        /// <summary>
        /// 是否计时器结束
        /// </summary>
        public bool IsDead()
        {
            if (!_isMarked)
            {
                LogMgr.Ins.LogError("【TimerInfo】[IsDead] Can not check dead before mark.");
                
                return true;
            }

            if (_cnt >= CntLimited)
                return true;

            return false;
        }
    }
}