using FairyGUI;

namespace Engine.Scripts.Runtime.View
{
    public abstract class CompSliderBase : GSlider, IComp
    {
        public void DoInit()
        {
            OnInit();
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

        protected abstract void OnInit();
        
        protected abstract void OnClose();
        
        protected abstract void OnDispose();

        protected abstract void InitChildren();
        
        protected abstract void OnInitChildren();
        
        protected abstract void OnCloseChildren();
        
        protected abstract void OnDisposeChildren();
    }
}