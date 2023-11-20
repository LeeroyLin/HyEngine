namespace Engine.Scripts.Runtime.System
{
    public abstract class SystemModelBase : ISystemModel
    {
        public void Init()
        {
            OnInit();
        }

        protected abstract void OnInit();
    }
}