namespace Engine.Scripts.Runtime.Entrance
{
    public abstract class EntranceBase
    {
        public void Start()
        {
            OnInit();
            OnStart();
        }
        
        public void DoUpdate()
        {
            OnUpdate();
        }
        
        public void DoLateUpdate()
        {
            OnLateUpdate();
        }
        
        public void DoFixedUpdate()
        {
            OnFixedUpdate();
        }

        protected abstract void OnInit();
        protected abstract void OnStart();
        protected abstract void OnUpdate();
        protected abstract void OnLateUpdate();
        protected abstract void OnFixedUpdate();
    }
}