using FairyGUI;

namespace Engine.Scripts.Runtime.View
{
    public abstract class CompLabelBase : GLabel, IComp
    {
        public void DoInit()
        {
            InitChildren();
            
            OnInitChildren();

            OnInit();
        }

        public void DoClose()
        {
            OnCloseChildren();

            OnClose();
        }

        public void DoDispose()
        {
            OnDisposeChildren();

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