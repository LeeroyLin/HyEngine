using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using UnityEngine;

namespace Engine.Scripts.Runtime.CameraStage
{
    public class CameraMgr : ManagerBase<CameraMgr>
    {
        private static readonly string CAM_POINT_NODE_NAME = "CamPoint";
        
        private ICameraStage _currStage;

        private ICameraStageGenerator _generator;

        private LogGroup _log;

        private Camera _camera;
        private Transform _camTrans;
        private Transform _pointTrans;
        
        public void Init(ICameraStageGenerator generator)
        {
            _generator = generator;
            
            _log = new LogGroup("CameraStageMgr");
        }

        protected override void OnReset()
        {
            // 关闭之前场景
            CloseCurr();
            
            _camera = null;
            _camTrans = null;

            RemovePointNode();
        }

        protected override void OnDisposed()
        {
            // 关闭之前场景
            CloseCurr();
            
            _camera = null;
            _camTrans = null;

            RemovePointNode();
        }

        void RemovePointNode()
        {
            if (_pointTrans != null)
            {
                Object.Destroy(_pointTrans.gameObject);
                _pointTrans = null;
            }
        }

        public void SetCameraNode(Transform node)
        {
            _camTrans = node;
            _camera = node.GetComponent<Camera>();

            _pointTrans = (new GameObject(CAM_POINT_NODE_NAME)).transform;
            _pointTrans.position = _camTrans.position;
        }

        public void ChangeStage(int key)
        {
            if (_currStage?.Key == key)
            {
                _log.Warning("[Open] Can not open same stage with key:'{0}'.", key);
                
                return;
            }
            
            // 关闭之前场景
            CloseCurr();
            
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

        public Transform GetPointTrans()
        {
            return _pointTrans;
        }

        public bool GetStage<T>(out T stage) where T : class, ICameraStage
        {
            if (_currStage != null)
            {
                stage = _currStage as T;
                return true;
            }

            stage = null;

            return false;
        }

        void CloseCurr()
        {
            _currStage?.Exit();
            _currStage = null;
        }
    }
}