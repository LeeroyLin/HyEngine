using Engine.Scripts.Runtime.Utils;
using FairyGUI;

namespace Engine.Scripts.Runtime.View
{
    public abstract class ViewBase : GComponent, IView
    {
        public string Pkg { get; private set; }
        public string Name { get; private set; }
        public string CustomKey { get; private set; }

        /// <summary>
        /// 是否在激活列表
        /// </summary>
        public bool IsActive { get; private set; }
        
        /// <summary>
        /// 非激活时的毫秒时间戳
        /// </summary>
        public long InactiveAt { get; private set; }

        public ViewBase(string pkg, string name)
        {
            Pkg = pkg;
            Name = name;
            CustomKey = ViewMgr.GetCustomKey(pkg, name);

            InactiveAt = 0;
            IsActive = true;
        }

        public void DoInit()
        {
            OnInit();
        }

        public void DoOpen(ViewArgsBase args = null)
        {
            OnOpen(args);
        }

        public void DoClose()
        {
            OnClose();
        }

        public void DoDispose()
        {
            OnDispose();
            
            Dispose();
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

        protected abstract void OnInit();
        
        protected abstract void OnOpen(ViewArgsBase args = null);
        
        protected abstract void OnClose();
        
        protected abstract void OnDispose();

        protected abstract void InitChildren();
    }
}