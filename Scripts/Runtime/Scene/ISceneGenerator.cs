namespace Engine.Scripts.Runtime.Scene
{
    public interface ISceneGenerator
    {
        SceneBase GetSceneIns(string key);
    }
}