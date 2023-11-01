using Engine.Scripts.Runtime.Utils;

namespace Engine.Scripts.Runtime.View
{
    public class ViewInfo
    {
        public ViewBase View { get; private set; }
        public long InactiveAt { get; private set; }
        public bool IsActive { get; private set; }
        
        public ViewInfo(ViewBase view)
        {
            View = view;

            InactiveAt = 0;
            IsActive = true;
        }

        /// <summary>
        /// 激活
        /// </summary>
        public void Active()
        {
            IsActive = true;
            InactiveAt = 0;
        }

        /// <summary>
        /// 非激活
        /// </summary>
        public void Inactive()
        {
            IsActive = false;
            InactiveAt = TimeUtil.GetTimestampMS();
        }

        /// <summary>
        /// 是否已经过期
        /// </summary>
        /// <param name="durationMS">过期所需毫秒</param>
        /// <returns></returns>
        public bool IsExpired(long durationMS)
        {
            var leftMS = TimeUtil.ExpireMS(InactiveAt + durationMS);

            return leftMS <= 0;
        }
    }
}