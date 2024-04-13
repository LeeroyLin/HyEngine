using System;
using System.Collections.Generic;
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
            if (IsEntered)
                return;

            void Handler()
            {
                try
                {
                    DoEnter(args);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SceneBase] {e.Message} \n {e.StackTrace}");
                }
            }
            
            bool isEnterNow = OnCheckEnter(Handler, args);

            if (isEnterNow)
                Handler();
        }

        public void Exit()
        {
            EventGroup.ClearCurrentAllEvents();
            
            CloseSystems();
            
            OnExit();

            IsEntered = false;
        }

        protected abstract void OnInit();
        
        /// <summary>
        /// 检测是否立即进入场景
        /// 返回false，不会立即进入场景
        /// </summary>
        /// <param name="doEnterHandler">用于主动进入场景的方法</param>
        /// <param name="args"></param>
        /// <returns>true：立即进入场景 false：等待主动进入场景</returns>
        protected abstract bool OnCheckEnter(Action doEnterHandler, SceneArgsBase args = null);
        protected abstract string[] OnGetPreLoadUIPkg();
        protected abstract void OnEnter(SceneArgsBase args = null);
        protected abstract void OnExit();
        protected abstract ISystem[] OnGetSystems();
        protected abstract void OnRegGameEvents();

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

        void StartSystems()
        {
            sysArr = OnGetSystems();

            foreach (var sys in sysArr)
                sys.Enter();
        }

        void CloseSystems()
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
    }
}