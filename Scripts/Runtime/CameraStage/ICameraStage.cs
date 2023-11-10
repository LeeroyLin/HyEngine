using UnityEngine;

namespace Engine.Scripts.Runtime.CameraStage
{
    public interface ICameraStage
    {
        void Exit();
        void Enter();
        void OnDrag(Vector2 dir);
        void OnZoom(float value);
    }
}