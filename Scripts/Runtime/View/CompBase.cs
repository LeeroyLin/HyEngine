using FairyGUI;

namespace Engine.Scripts.Runtime.View
{
    public abstract class CompBase : GComponent, IComp
    {
        public void DoInit()
        {
        }

        public void DoClose()
        {
        }

        public void DoDispose()
        {
        }

        protected abstract void OnInit();
        
        protected abstract void OnClose();
        
        protected abstract void OnDispose();
        
        protected abstract void InitChildren();
    }
}