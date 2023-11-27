using Engine.Scripts.Runtime.Event;

namespace Engine.Scripts.Runtime.System
{
    public abstract class SystemModelBase : ISystemModel
    {
        public void Init()
        {
            OnInit();
        }

        protected abstract void OnInit();
        
        /// <summary>
        /// 同步广播
        /// </summary>
        /// <param name="data"></param>
        public void Broadcast(IEventData data)
        {
            EventMgr.Ins.Broadcast(EEventGroup.GameLogic, data);
        }

        /// <summary>
        /// 异步同步广播
        /// </summary>
        /// <param name="data"></param>
        public void BroadcastAsync(IEventData data)
        {
            EventMgr.Ins.BroadcastAsync(EEventGroup.GameLogic, data);
        }
    }
}