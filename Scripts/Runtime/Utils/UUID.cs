using UnityEngine;

namespace Engine.Scripts.Runtime.Utils
{
    public class UUID
    {
        public static int Count;
        public static int CountTemp;
        public static long LastTimestamp;

        /// <summary>
        /// pre在3位以下
        /// </summary>
        /// <param name="pre"></param>
        /// <returns></returns>
        public static ulong GetUUID(int pre = 0)
        {
            var timestamp = TimeUtilBase.GetTimestamp();
            if (LastTimestamp != timestamp)
            {
                LastTimestamp = timestamp;
                Count = 0;
            }
            else
                Count++;

            // pre：3位 秒级时间戳去掉第一位：9位 计次：7位
            return (ulong)(pre * 10000000000000000 + (LastTimestamp - 1000000000) * 10000000 + Count);
        }

        public static int GetUUIDTemp()
        {
            if (CountTemp == int.MinValue)
                CountTemp = 0;
            else
                CountTemp--;
            
            return CountTemp;
        }
    }
}