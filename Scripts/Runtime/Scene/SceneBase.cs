using Engine.Scripts.Runtime.Resource;

namespace Engine.Scripts.Runtime.Scene
{
    public abstract class SceneBase : ISceneBase
    {
        public string Key { get; private set; }

        public SceneBase(string key)
        {
            Key = key;
        }
        
        public void Init()
        {
            OnInit();
        }

        public void Enter(SceneArgsBase args = null)
        {
            PreLoadUIPkg();
            
            OnEnter(args);
        }

        public void Exit()
        {
            OnExit();
        }

        protected abstract void OnInit();
        protected abstract string[] OnGetPreLoadUIPkg();
        protected abstract void OnEnter(SceneArgsBase args = null);
        protected abstract void OnExit();

        void PreLoadUIPkg()
        {
            var pkgs = OnGetPreLoadUIPkg();

            foreach (var pkg in pkgs)
                ResMgr.Ins.AddPackage(pkg);
        }
    }
}