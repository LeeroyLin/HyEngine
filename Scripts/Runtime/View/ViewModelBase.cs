using Engine.Scripts.Runtime.Event;

namespace Engine.Scripts.Runtime.View
{
    public abstract class ViewModelBase : IViewModel
    {
        public EventGroup EventGroup { get; private set; }

        public ViewModelBase()
        {
            EventGroup = new EventGroup(EEventGroup.GameLogic);
        }

        public abstract void Init();
    }
}