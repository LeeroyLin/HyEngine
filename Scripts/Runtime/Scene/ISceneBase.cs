namespace Engine.Scripts.Runtime.Scene
{
    public interface ISceneBase
    {
        void Init();
        
        void Enter(SceneArgsBase args = null);
        
        void Exit();
    }
}