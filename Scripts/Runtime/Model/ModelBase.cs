using Engine.Scripts.Runtime.Event;

namespace Engine.Scripts.Runtime.Model
{
    public abstract class ModelBase : IModel
    {
        protected EventGroup NetEventGroup { get; private set; }
        protected EventGroup GameEventGroup { get; private set; }
        
        public ModelBase()
        {
            NetEventGroup = new EventGroup(EEventGroup.Net);
            GameEventGroup = new EventGroup(EEventGroup.GameLogic);
            
            OnRegNetEvents();
            OnRegGameEvents();
        }
        
        public void Reset()
        {
            NetEventGroup.ClearCurrentAllEvents();
            GameEventGroup.ClearCurrentAllEvents();
        }

        protected abstract void InitData();
        protected abstract void OnRegNetEvents();
        protected abstract void OnRegGameEvents();
    }
}