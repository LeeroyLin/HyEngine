using UnityEngine;

namespace Engine.Scripts.Runtime.Cfg
{
    public struct IntW
    {
        /// <summary>
        /// 整数原值
        /// </summary>
        public int IntOri { get; }
        
        /// <summary>
        /// 原值除以10000之后向上取整的值
        /// </summary>
        public int IntCalCeil { get; }
        
        /// <summary>
        /// 原值除以10000之后向下取整的值
        /// </summary>
        public int IntCalFloor { get; }
        
        /// <summary>
        /// 原值除以10000之后四舍五入取整的值
        /// </summary>
        public int IntCalRound { get; }
        
        /// <summary>
        /// 原值除以10000后的浮点数值
        /// </summary>
        public float FloatCal { get; }
        
        /// <summary>
        /// 百分比数值，保留2位小数
        /// </summary>
        public string PercentStr => (IntOri / 100f).ToString("f2");
        
        /// <summary>
        /// 百分比数值，没有小数
        /// </summary>
        public string PercentIntStr => IntOri / 100 + "";

        public IntW(int value)
        {
            IntOri = value;
            IntCalCeil = Mathf.CeilToInt(value / 10000f);
            IntCalFloor = Mathf.FloorToInt(value / 10000f);
            IntCalRound = Mathf.RoundToInt(value / 10000f);
            FloatCal = value / 10000f;
        }
    }
}