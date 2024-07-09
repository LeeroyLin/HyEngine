using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;

namespace Engine.Scripts.Runtime.Scene
{
    public class SceneMgr : ManagerBase<SceneMgr>
    {
        private SceneBase _current = null;

        private ISceneGenerator _generator;

        private LogGroup _log;
        
        public void Init(ISceneGenerator generator)
        {
            _generator = generator;
            
            _log = new LogGroup("SceneMgr");
        }

        protected override void OnReset()
        {
            // 关闭之前场景
            CloseCurr();
        }

        protected override void OnDisposed()
        {
            // 关闭之前场景
            CloseCurr();
        }

        /// <summary>
        /// 打开场景
        /// </summary>
        /// <param name="key">场景名</param>
        /// <param name="args">参数，可空</param>
        public void Open(string key, SceneArgsBase args = null)
        {
            if (_current != null && _current.Key == key)
            {
                _current.ReEnter();

                return;
            }
            
            // 关闭之前场景
            CloseCurr();
            
            // 获得新场景实例
            _current = _generator.GetSceneIns(key);

            // 回调
            _current.Init();
            _current.Enter(args);
        }

        private void CloseCurr()
        {
            _current?.Exit();
            _current = null;
        }
    }
}