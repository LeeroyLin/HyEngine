using System;
using Engine.Scripts.Runtime.Event;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.System;

namespace Engine.Scripts.Runtime.Scene
{
    public abstract class SceneBase : ISceneBase
    {
        public string Key { get; private set; }

        private ISystem[] sysArr;
        
        public EventGroup EventGroup { get; private set; }
        
        public SceneBase(string key)
        {
            Key = key;
            
            EventGroup = new EventGroup(EEventGroup.GameLogic);
        }
        
        public void Init()
        {
            OnInit();
        }

        public void Enter(SceneArgsBase args = null)
        {
            PreLoadUIPkg();
            
            OnRegGameEvents();

            StartSystems();
            
            OnEnter(args);
        }

        public void Exit()
        {
            EventGroup.ClearCurrentAllEvents();
            
            CloseSystems();
            
            OnExit();
        }

        protected abstract void OnInit();
        protected abstract string[] OnGetPreLoadUIPkg();
        protected abstract void OnEnter(SceneArgsBase args = null);
        protected abstract void OnExit();
        protected abstract ISystem[] OnGetSystems();
        protected abstract void OnRegGameEvents();

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

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        protected void Reg<T>(Action<T> callback) where T : IEventData
        {
            EventGroup.Reg(callback);
        }
        
        /// <summary>
        /// 同步广播
        /// </summary>
        /// <param name="data"></param>
        protected void Broadcast(IEventData data)
        {
            EventGroup.Broadcast(data);
        }

        /// <summary>
        /// 异步同步广播
        /// </summary>
        /// <param name="data"></param>
        protected void BroadcastAsync(IEventData data)
        {
            EventGroup.BroadcastAsync(data);
        }
    }
}