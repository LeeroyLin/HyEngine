using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Utils;

namespace Engine.Scripts.Runtime.Scene
{
    public class SceneMgr : SingletonClass<SceneMgr>, IManager
    {
        private SceneBase _current = null;

        private ISceneGenerator _generator;

        private LogGroup _log;        
        
        public void Reset()
        {
        }

        public void Init(ISceneGenerator generator)
        {
            _generator = generator;
            
            _log = new LogGroup("SceneMgr");
        }

        /// <summary>
        /// 打开场景
        /// </summary>
        /// <param name="key">场景名</param>
        /// <param name="args">参数，可空</param>
        public void Open(string key, SceneArgsBase args = null)
        {
            if (_current?.Key == key)
            {
                _log.Warning("[Open] Can not open same scene with key:'{0}'.", key);
                
                return;
            }
            
            // 关闭之前场景
            _current?.Exit();
            
            // 获得新场景实例
            _current = _generator.GetSceneIns(key);

            // 回调
            _current.Init();
            _current.Enter(args);
        }
    }
}