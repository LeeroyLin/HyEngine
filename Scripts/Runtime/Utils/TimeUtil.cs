﻿using System;
using UnityEngine;

namespace Engine.Scripts.Runtime.Utils
{
    public static partial class TimeUtil
    {
        /**
         * 获得时间戳 秒级
         */
        public static long GetTimestamp()
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 1970年1月1日零时
            long timestamp = (long)(DateTime.UtcNow.Subtract(startTime)).TotalSeconds;
            return timestamp;
        }

        /**
         * 获得时间戳 毫秒级
         */
        public static long GetTimestampMS()
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 1970年1月1日零时
            long timestamp = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
            return timestamp;
        }

        /**
         * 距离某秒级时间，已经经过多少时间
         */
        public static int Expire(long time)
        {
            var now = GetTimestamp();
            return Mathf.Max(0, (int)(now - time));
        }

        /**
         * 距离某毫秒级时间，已经经过多少时间
         */
        public static int ExpireMS(long timeMS)
        {
            var nowMS = GetTimestampMS();
            return Mathf.Max(0, (int)(nowMS - timeMS));
        }

        /**
         * 距离某秒级时间，还剩多久
         */
        public static int Left(long time)
        {
            var now = GetTimestamp();
            return Mathf.Max(0, (int)(time - now));
        }

        /**
         * 距离某毫秒级时间，还剩多久
         */
        public static int LeftMS(long timeMS)
        {
            var nowMS = GetTimestampMS();
            return Mathf.Max(0, (int)(timeMS - nowMS));
        }
    }
}