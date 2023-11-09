namespace Engine.Scripts.Runtime.CameraStage
{
    public abstract class CameraStageBase : ICameraStage
    {
        public string Key { get; private set; }

        public void Enter()
        {
        }
        
        public void Exit()
        {
        }

        protected abstract void OnEnter();
        protected abstract void OnExit();
    }
}