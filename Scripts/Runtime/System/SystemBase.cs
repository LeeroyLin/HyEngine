namespace Engine.Scripts.Runtime.System
{
    public abstract class SystemBase<T> : ISystem where T : SystemModelBase, new()
    {
        public T SystemModel { get; private set; }

        public SystemBase()
        {
            SystemModel = new T();
            
            SystemModel.Init();
        }
        
        public void Enter()
        {
            OnEnter();
        }

        public void Exit()
        {
            OnExit();
        }

        protected abstract void OnEnter();

        protected abstract void OnExit();
    }
}