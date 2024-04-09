using UnityEngine;

namespace Engine.Scripts.Runtime.Utils
{
    public class MathUtil
    {
        /// <summary>
        /// 矩形是否相交
        /// </summary>
        /// <param name="ltPos1"></param>
        /// <param name="rbPos1"></param>
        /// <param name="ltPos2"></param>
        /// <param name="rbPos2"></param>
        /// <returns></returns>
        public static bool IsRectCross(Vector2 ltPos1, Vector2 rbPos1, Vector2 ltPos2, Vector2 rbPos2)
        {
            var minX = Mathf.Max(ltPos1.x, ltPos2.x);
            var minY = Mathf.Max(rbPos1.y, rbPos2.y);
            var maxX = Mathf.Min(rbPos1.x, rbPos2.x);
            var maxY = Mathf.Min(ltPos1.y, ltPos2.y);

            return minX <= maxX && minY <= maxY;
        }
        
        /// <summary>
        /// 点是否在矩形内
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="ltPos"></param>
        /// <param name="rbPos"></param>
        /// <returns></returns>
        public static bool IsPointInRect(Vector2 pos, Vector2 ltPos, Vector2 rbPos)
        {
            if (pos.x < ltPos.x)
                return false;
            
            if (pos.x > rbPos.x)
                return false;
            
            if (pos.y < rbPos.y)
                return false;
            
            if (pos.y > ltPos.y)
                return false;

            return true;
        }

        /// <summary>
        /// 获得整数 右边几位
        /// 123456 右边3位 456
        /// </summary>
        /// <param name="val"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static int GetIntLast(int val, int num)
        {
            int v = 1;
            for (int i = 0; i < num; i++)
                v *= 10;
            
            return val - val / v * v;
        }

        public static long Min(long val1, long val2)
        {
            return val1 < val2 ? val1 : val2;
        }

        public static long Max(long val1, long val2)
        {
            return val1 > val2 ? val1 : val2;
        }

        public static int Long2NearInt(long val)
        {
            if (val > int.MaxValue)
                return int.MaxValue;

            if (val < int.MinValue)
                return int.MinValue;

            return (int) val;
        }
    }
}