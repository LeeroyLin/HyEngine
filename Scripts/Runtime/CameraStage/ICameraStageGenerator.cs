namespace Engine.Scripts.Runtime.CameraStage
{
    public interface ICameraStageGenerator
    {
        CameraStageBase GetStageIns(string key);
    }
}