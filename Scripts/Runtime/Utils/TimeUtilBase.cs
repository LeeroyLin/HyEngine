using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Engine.Scripts.Runtime.Utils
{
    public class TimeUtilBase
    {
        /// <summary>
        /// 本地转服务器时间的差量
        /// </summary>
        public static long Local2ServerTimeMSOffset
        {
            get
            {
                long sum = 0;

                if (serverTimeOffsetMSQueue.Count == 0)
                    return sum;
                
                foreach (var data in serverTimeOffsetMSQueue)
                {
                    sum += data;
                }

                sum /= serverTimeOffsetMSQueue.Count;

                return sum;
            }
        }

        /// <summary>
        /// 本地转服务器时间的差量 队列
        /// </summary>
        private static Queue<long> serverTimeOffsetMSQueue = new Queue<long>();

        private static readonly Dictionary<string, int> TIME_FORMAT_DIC = new Dictionary<string, int>()
        {
            {"D", 86400},
            {"H", 3600},
            {"M", 60},
            {"S", 0},
        };

        class TimeFormatInfo
        {
            public string key;
            public bool isHas;
            public int value;

            public TimeFormatInfo(string key, bool isHas, int value)
            {
                this.key = key;
                this.isHas = isHas;
                this.value = value;
            }
        }

        /// <summary>
        /// 设置服务器毫秒时间
        /// </summary>
        /// <param name="serverMSTime"></param>
        public static void SetServerTimeMS(long serverMSTime)
        {
            long offset = serverMSTime - GetLocalTimeMS();

            if (serverTimeOffsetMSQueue.Count >= 10)
                serverTimeOffsetMSQueue.Dequeue();
            
            serverTimeOffsetMSQueue.Enqueue(offset);
        }
        
        /// <summary>
        /// 获得时间戳 秒级
        /// </summary>
        /// <returns></returns>
        public static long GetTimestamp()
        {
            return GetTimestampMS() / 1000;
        }

        /// <summary>
        /// 获得时间戳 毫秒级
        /// </summary>
        /// <returns></returns>
        public static long GetTimestampMS()
        {
            return GetLocalTimeMS() + Local2ServerTimeMSOffset;
        }

        /// <summary>
        /// 距离某秒级时间，已经经过多少时间
        /// </summary>
        /// <param name="time">秒级时间戳</param>
        /// <returns></returns>
        public static int Expire(long time)
        {
            var now = GetTimestamp();
            return Mathf.Max(0, MathUtil.Long2NearInt(now - time));
        }

        /// <summary>
        /// 距离某秒级时间，已经经过多少时间
        /// </summary>
        /// <param name="time">秒级时间戳</param>
        /// <returns></returns>
        public static long ExpireLong(long time)
        {
            var now = GetTimestamp();
            var delta = now - time;
            if (delta < 0)
                delta = 0;
            return delta;
        }

        /// <summary>
        /// 距离某毫秒级时间，已经经过多少毫秒级时间
        /// </summary>
        /// <param name="timeMS">毫秒级时间戳</param>
        /// <param name="isLocalTime">本地时间计算</param>
        /// <returns></returns>
        public static int ExpireMS(long timeMS, bool isLocalTime = false)
        {
            var nowMS = isLocalTime ? GetLocalTimeMS() : GetTimestampMS();
            return Mathf.Max(0, MathUtil.Long2NearInt(nowMS - timeMS));
        }

        /// <summary>
        /// 距离某毫秒级时间，已经经过多少毫秒级时间
        /// </summary>
        /// <param name="timeMS">毫秒级时间戳</param>
        /// <param name="isLocalTime">本地时间计算</param>
        /// <returns></returns>
        public static long ExpireMSLong(long timeMS, bool isLocalTime = false)
        {
            var nowMS = isLocalTime ? GetLocalTimeMS() : GetTimestampMS();
            var delta = nowMS - timeMS;
            if (delta < 0)
                delta = 0;
            return delta;
        }

        /// <summary>
        /// 距离某毫秒级时间，已经经过多少秒级时间
        /// </summary>
        /// <param name="timeMS">毫秒级时间戳</param>
        /// <param name="isLocalTime">本地时间计算</param>
        /// <returns></returns>
        public static int ExpireByMS(long timeMS, bool isLocalTime = false)
        {
            var nowMS = isLocalTime ? GetLocalTimeMS() : GetTimestampMS();
            var sec = (nowMS - timeMS) / 1000;
            return Mathf.Max(0, MathUtil.Long2NearInt(sec));
        }

        /// <summary>
        /// 距离某毫秒级时间，已经经过多少秒级时间
        /// </summary>
        /// <param name="timeMS">毫秒级时间戳</param>
        /// <param name="isLocalTime">本地时间计算</param>
        /// <returns></returns>
        public static long ExpireByMSLong(long timeMS, bool isLocalTime = false)
        {
            var nowMS = isLocalTime ? GetLocalTimeMS() : GetTimestampMS();
            var delta = (nowMS - timeMS) / 1000;
            if (delta < 0)
                delta = 0;
            return delta;
        }

        /// <summary>
        /// 距离某秒级时间，还剩多久
        /// </summary>
        /// <param name="time">秒级时间戳</param>
        /// <returns></returns>
        public static int Left(long time)
        {
            var now = GetTimestamp();
            return Mathf.Max(0, MathUtil.Long2NearInt(time - now));
        }

        /// <summary>
        /// 距离某秒级时间，还剩多久
        /// </summary>
        /// <param name="time">秒级时间戳</param>
        /// <returns></returns>
        public static long LeftLong(long time)
        {
            var now = GetTimestamp();
            var delta = time - now;
            if (delta < 0)
                delta = 0;
            return delta;
        }

        /// <summary>
        /// 距离某毫秒级时间，还剩多久毫秒级时间
        /// </summary>
        /// <param name="timeMS">毫秒级时间戳</param>
        /// <param name="isLocalTime">本地时间计算</param>
        /// <returns></returns>
        public static int LeftMS(long timeMS, bool isLocalTime = false)
        {
            var nowMS = isLocalTime ? GetLocalTimeMS() : GetTimestampMS();
            return Mathf.Max(0, MathUtil.Long2NearInt(timeMS - nowMS));
        }

        /// <summary>
        /// 距离某毫秒级时间，还剩多久毫秒级时间
        /// </summary>
        /// <param name="timeMS">毫秒级时间戳</param>
        /// <param name="isLocalTime">本地时间计算</param>
        /// <returns></returns>
        public static long LeftMSLong(long timeMS, bool isLocalTime = false)
        {
            var nowMS = isLocalTime ? GetLocalTimeMS() : GetTimestampMS();
            var delta = timeMS - nowMS;
            if (delta < 0)
                delta = 0;
            return delta;
        }

        /// <summary>
        /// 距离某毫秒级时间，还剩多久秒级时间
        /// </summary>
        /// <returns></returns>
        public static int LeftByMS(long timeMS, bool isLocalTime = false)
        {
            var nowMS = isLocalTime ? GetLocalTimeMS() : GetTimestampMS();
            var sec = (timeMS - nowMS) / 1000;
            return Mathf.Max(0, MathUtil.Long2NearInt(sec));
        }

        /// <summary>
        /// 距离某毫秒级时间，还剩多久秒级时间
        /// </summary>
        /// <returns></returns>
        public static long LeftByMSLong(long timeMS, bool isLocalTime = false)
        {
            var nowMS = isLocalTime ? GetLocalTimeMS() : GetTimestampMS();
            var delta = (timeMS - nowMS) / 1000;
            if (delta < 0)
                delta = 0;
            return delta;
        }

        /// <summary>
        /// 根据格式化字符串内容，格式化时间显示
        /// </summary>
        /// <param name="sec">秒</param>
        /// <param name="isHideLeftZero">是否隐藏左侧0值 显示15:00 非00:15:00</param>
        /// <param name="pattern">格式化字符串, D:天 H:小时 M:分钟 S:秒</param>
        /// <param name="dayUnit">天，单位字符串。显示在天数后，例如："1d:5:23:1"</param>
        /// <returns></returns>
        public static string GetFormatStr(int sec, bool isHideLeftZero = false, string pattern = "DHMS", string dayUnit = "天")
        {
            StringBuilder sb = new StringBuilder();
            
            // 格式名 对应 格式信息 字典
            Dictionary<string, TimeFormatInfo> dic = new Dictionary<string, TimeFormatInfo>();

            // 格式信息列表 按格式对应时间切换值倒序排序
            List<TimeFormatInfo> list = new List<TimeFormatInfo>();

            foreach (var info in TIME_FORMAT_DIC)
            {
                bool isHas = pattern.Contains(info.Key);
                var data = new TimeFormatInfo(info.Key, isHas, 0);
                dic.Add(info.Key, data);
                
                list.Add(data);
            }
            
            // 按格式对应时间切换值倒序排序
            list.Sort((a, b) =>
            {
                var aVal = TIME_FORMAT_DIC[a.key];
                var bVal = TIME_FORMAT_DIC[b.key];
                return bVal.CompareTo(aVal);
            });

            foreach (var data in list)
            {
                int val = TIME_FORMAT_DIC[data.key];

                if (sec < val)
                    continue;

                int num = val > 0 ? sec / val : sec;
                data.value = num;

                sec -= num * val;
            }

            bool isShowAny = false;
            
            for (int i = 0; i < list.Count; i++)
            {
                var data = list[i];
                
                if (data.key == "D")
                {
                    if (data.value > 0)
                    {
                        sb.Append(data.value);
                        sb.Append(dayUnit);
                        sb.Append(":");

                        isShowAny = true;
                    }
                }
                else
                {
                    var v = data.value.ToString().PadLeft(2, '0');

                    if (isShowAny || !isHideLeftZero || data.value > 0)
                    {
                        sb.Append(v);

                        if (i < list.Count - 1)
                            sb.Append(":");
                        
                        isShowAny = true;
                    }
                }
            }
            
            return sb.ToString();
        }

        // 获得本地时间戳
        public static long GetLocalTimeMS()
        {
            DateTime startTime = new DateTime(1970, 1, 1); // 1970年1月1日零时
            long timestamp = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;

            return timestamp;
        }
    }
}