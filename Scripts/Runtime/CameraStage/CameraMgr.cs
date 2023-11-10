using Client.Scripts.Runtime.CameraStage;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.Utils;
using UnityEngine;

namespace Engine.Scripts.Runtime.CameraStage
{
    public class CameraMgr : SingletonClass<CameraMgr>, IManager
    {
        private CameraStageBase _currStage;

        private ICameraStageGenerator _generator;

        private LogGroup _log;

        private Camera _camera;
        private Transform _camTrans;
        
        public void Reset()
        {
        }

        public void Init(ICameraStageGenerator generator)
        {
            _generator = generator;
            
            _log = new LogGroup("CameraStageMgr");
            
            CreateCamara();
        }

        public void ChangeStage(string key)
        {
            if (_currStage?.Key == key)
            {
                _log.Warning("[Open] Can not open same stage with key:'{0}'.", key);
                
                return;
            }
            
            // 关闭之前场景
            _currStage?.Exit();
            
            // 获得新场景实例
            _currStage = _generator.GetStageIns(key);

            // 回调
            _currStage.Enter();
        }

        public Camera GetCamera()
        {
            return _camera;
        }

        public Transform GetCameraTrans()
        {
            return _camTrans;
        }

        public MainCameraStage GetMainCameraStage()
        {
            if (_currStage != null && _currStage.Key == ECameraStage.Main)
            {
                return _currStage as MainCameraStage;
            }

            return null;
        }

        public void Drag(Vector2 dir)
        {
            _currStage?.OnDrag(dir);
        }

        public void Zoom(float value)
        {
            _currStage?.OnZoom(value);
        }

        void CreateCamara()
        {
            var obj = ResMgr.Ins.GetAsset<GameObject>("Camera\\Main Camera.prefab");
            obj.name = "Main Camera";
            
            _camera = obj.GetComponent<Camera>();
            _camTrans = obj.transform;
        }
    }
}