using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.Utils;
using UnityEngine;

namespace Engine.Scripts.Runtime.CameraStage
{
    public class CameraMgr : SingletonClass<CameraMgr>, IManager
    {
        private ICameraStage _currStage;

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
        }

        public void SetCameraNode(Transform node)
        {
            _camTrans = node;
            _camera = node.GetComponent<Camera>();
        }

        public void ChangeStage(int key)
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

        public bool GetStage<T>(out T stage) where T : class, ICameraStage
        {
            if (_currStage != null && _currStage.Key.GetType() == typeof(T))
            {
                stage = _currStage as T;
                return true;
            }

            stage = null;

            return false;
        }
    }
}