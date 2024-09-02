using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Event;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.System;
using UnityEngine;

namespace Engine.Scripts.Runtime.Scene
{
    public abstract class SceneBase : ISceneBase
    {
        public string Key { get; private set; }

        private ISystem[] sysArr;
        
        public EventGroup EventGroup { get; private set; }
        
        public bool IsEntered { get; private set; }
        
        protected SceneArgsBase Args { get; private set; }
        
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
            Args = args;

            if (IsEntered)
                return;

            bool isEnterNow = OnCheckEnter();

            if (isEnterNow)
                TryDoEnter();
        }

        protected void TryDoEnter()
        {
            try
            {
                DoEnter(Args);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneBase] {e.Message} \n {e.StackTrace}");
            }
        }

        public void Exit()
        {
            EventGroup.ClearCurrentAllEvents();
            
            CloseSystems();
            
            OnExit();

            IsEntered = false;
        }

        public void ReEnter()
        {
            OnReEnter();
        }

        protected abstract void OnInit();
        
        /// <summary>
        /// 检测是否立即进入场景
        /// 返回false，不会立即进入场景
        /// </summary>
        /// <returns>true：立即进入场景 false：等待主动进入场景</returns>
        protected abstract bool OnCheckEnter();
        protected abstract string[] OnGetPreLoadUIPkg();
        protected abstract void OnEnter(SceneArgsBase args = null);
        protected abstract void OnExit();
        protected abstract ISystem[] OnGetSystems();
        protected abstract void OnRegGameEvents();
        protected virtual void OnReEnter() {}

        /// <summary>
        /// 执行进入
        /// </summary>
        void DoEnter(SceneArgsBase args)
        {
            if (IsEntered)
                return;

            IsEntered = true;

            PreLoadUIPkg();
            
            OnRegGameEvents();

            StartSystems();
            
            OnEnter(args);
        }

        void PreLoadUIPkg()
        {
            var pkgs = OnGetPreLoadUIPkg();

            foreach (var pkg in pkgs)
                ResMgr.Ins.AddPackage(pkg);
        }

        protected void StartSystems()
        {
            sysArr = OnGetSystems();

            foreach (var sys in sysArr)
                sys.Enter();
        }

        protected void CloseSystems()
        {
            if (sysArr == null)
                return;
            
            foreach (var sys in sysArr)
                sys.Exit();
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
        /// 取消注册
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        protected void UnReg<T>(Action<T> callback) where T : IEventData
        {
            EventGroup.UnReg(callback);
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

        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="finished"></param>
        /// <param name="onProgress"></param>
        protected void PreloadAssets(List<string> assets, Action finished, Action<int, int> onProgress)
        {
            if (finished == null)
                return;
            
            int num = assets.Count;

            if (num <= 0)
            {
                finished();
                return;
            }
            
            int cnt = 0;

            foreach (var relPath in assets)
            {
                PoolMgr.Ins.GetAsync(relPath, o =>
                {
                    PoolMgr.Ins.SetNeedHide(relPath, false);
                    PoolMgr.Ins.Set(o);
                    cnt++;
                    
                    onProgress?.Invoke(cnt, num);

                    if (cnt == num)
                        finished();
                });
            }
        }

        protected void PreloadABs(List<string> relPathList, Action finished, Action<int, int> onProgress)
        {
            if (finished == null)
                return;
            
            int num = relPathList.Count;

            if (num <= 0)
            {
                finished();
                return;
            }
            
            int cnt = 0;

            foreach (var relPath in relPathList)
            {
                var abName = ResMgr.Ins.RelPath2ABName(relPath, out var isInGame, out var isPackage);
                
                Debug.Log($"CCC relPath:{relPath} abName:{abName}");
                
                ResMgr.Ins.LoadABAsyncWithABName(abName, isInGame, isPackage, ab =>
                {
                    cnt++;
                    
                    onProgress?.Invoke(cnt, num);

                    if (cnt == num)
                        finished();
                });
            }
        }
    }
}