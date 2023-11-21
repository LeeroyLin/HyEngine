namespace Engine.Scripts.Runtime.CameraStage
{
    public interface ICameraStage
    {
        int Key { get; protected set; }
        
        void Exit();
        void Enter();
    }
}