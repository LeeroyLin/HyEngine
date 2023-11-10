using UnityEngine;

namespace Engine.Scripts.Runtime.CameraStage
{
    public abstract class CameraStageBase : ICameraStage
    {
        public string Key { get; private set; }
        public float MinZoom { get; protected set; } = 2;
        public float MaxZoom { get; protected set; } = 8;
        public float ZoomSpeed { get; protected set; } = 2;

        public void Enter()
        {
        }
        
        public void Exit()
        {
        }

        public virtual void OnDrag(Vector2 dir)
        {
            var cam = CameraMgr.Ins.GetCameraTrans();
            var pos = cam.position;
            
            pos.x -= dir.x;
            pos.y -= dir.y;

            cam.position = pos;
        }

        public virtual void OnZoom(float value)
        {
            var cam = CameraMgr.Ins.GetCamera();
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - value * ZoomSpeed, MinZoom, MaxZoom);
        }

        protected abstract void OnEnter();
        protected abstract void OnExit();
    }
}