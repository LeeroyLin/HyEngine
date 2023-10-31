namespace Engine.Scripts.Runtime.Entrance
{
    public abstract class EntranceBase
    {
        public void Start()
        {
            OnInit();
            OnStart();
        }
        
        public void Tick(float dt)
        {
            OnTick(dt);
        }

        protected abstract void OnInit();
        protected abstract void OnStart();
        protected abstract void OnTick(float dt);
    }
}