using FairyGUI;

namespace Engine.Scripts.Runtime.View
{
    public abstract class CompProgressBarBase : GProgressBar, IComp
    {
        public CompProgressBarBase()
        {
            onCreated = DoInit;
        }

        void DoInit()
        {
            InitChildren();
            
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
        
        protected abstract void OnCloseChildren();
        
        protected abstract void OnDisposeChildren();
    }
}