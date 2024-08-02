using System.Collections.Generic;
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

        /// <summary>
        /// 判断点是否在多边形内
        /// 射线法 - 点水平向右发射线，与边相交点数为奇数则在多边形内（凸，凹多边形都可以用）
        /// </summary>
        /// <param name="targetWPos"></param>
        /// <param name="points">多边形点</param>
        /// <param name="horiLineWidth">用于检测的水平线段长度（在多边形最大水平宽度以上）</param>
        /// <returns></returns>
        public static bool CheckPosInPolygon(Vector3 targetWPos, List<Vector2> points, float horiLineWidth = 20)
        {
            Vector2 horiLineStart = targetWPos;
            Vector2 horiLineEnd = new Vector2(targetWPos.x + horiLineWidth, targetWPos.y);

            // 交点数
            var intersectCnt = 0;
            
            for (int i = 0; i < points.Count; i++)
            {
                var p1 = points[i];
                var p2 = Vector2.zero;
                
                // 最后一个点
                if (i == points.Count - 1)
                    p2 = points[0];
                else
                    p2 = points[i + 1];
                
                // 是否相交
                if (IsSegmentIntersect(horiLineStart, horiLineEnd, p1, p2))
                    intersectCnt++;
            }
            
            return intersectCnt % 2 == 1;
        }
        
        /// <summary>
        /// 两线段是否相交
        /// 依次判断线段旋转方向
        /// 该方法当线段端点在另一条线上不属于相交
        /// </summary>
        /// <returns></returns>
        public static bool IsSegmentIntersect(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End)
        {
            float d1 = SegmentRotateDirCheck(line1Start, line1End, line2Start);
            float d2 = SegmentRotateDirCheck(line1Start, line1End, line2End);
            float d3 = SegmentRotateDirCheck(line2Start, line2End, line1Start);
            float d4 = SegmentRotateDirCheck(line2Start, line2End, line1End);

            return d1 * d2 < 0 && d3 * d4 < 0;
        }

        /// <summary>
        /// 判断 线段[point, end1] 旋转到 线段[point, end2] 的方向
        /// </summary>
        /// <param name="point"></param>
        /// <param name="end1"></param>
        /// <param name="end2"></param>
        /// <returns></returns>
        static float SegmentRotateDirCheck(Vector2 point, Vector2 end1, Vector2 end2)
        {
            float x1 = end1.x - point.x;
            float y1 = end1.y - point.y;
            float x2 = end2.x - point.x;
            float y2 = end2.y - point.y;
            
            return x1 * y2 - x2 * y1;
        }
    }
}