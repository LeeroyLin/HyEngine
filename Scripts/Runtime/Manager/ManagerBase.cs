using Engine.Scripts.Runtime.Utils;

namespace Engine.Scripts.Runtime.Manager
{
    public abstract class ManagerBase<T> : SingletonClass<T>, IManager where T:class, new()
    {
        public bool IsDisposed { get; private set; }

        public void InitMgr()
        {
            IsDisposed = false;
        }
        
        public void Reset()
        {
            if (IsDisposed)
                return;
            
            OnReset();
        }
        
        public void Dispose()
        {
            if (IsDisposed)
                return;
            
            IsDisposed = true;
        }
        
        protected abstract void OnReset();

        protected abstract void OnDisposed();
    }
}