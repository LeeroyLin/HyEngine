using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.System;

namespace Engine.Scripts.Runtime.Scene
{
    public abstract class SceneBase : ISceneBase
    {
        public string Key { get; private set; }

        private ISystem[] sysArr;
        
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

            StartSystems();
            
            OnEnter(args);
        }

        public void Exit()
        {
            CloseSystems();
            
            OnExit();
        }

        protected abstract void OnInit();
        protected abstract string[] OnGetPreLoadUIPkg();
        protected abstract void OnEnter(SceneArgsBase args = null);
        protected abstract void OnExit();
        protected abstract ISystem[] OnGetSystems();

        void PreLoadUIPkg()
        {
            var pkgs = OnGetPreLoadUIPkg();

            foreach (var pkg in pkgs)
                ResMgr.Ins.AddPackage(pkg);
        }

        void StartSystems()
        {
            sysArr = OnGetSystems();

            foreach (var sys in sysArr)
            {
                sys.Enter();
            }
        }

        void CloseSystems()
        {
            sysArr = OnGetSystems();
            
            foreach (var sys in sysArr)
            {
                sys.Exit();
            }
        }
    }
}