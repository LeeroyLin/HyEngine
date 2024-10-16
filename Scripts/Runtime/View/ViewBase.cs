﻿using Engine.Scripts.Runtime.Global;
using Engine.Scripts.Runtime.Utils;
using FairyGUI;

namespace Engine.Scripts.Runtime.View
{
    public abstract class ViewBase : GComponent, IView
    {
        public string Pkg { get; private set; }
        public string Name { get; private set; }
        public string CustomKey { get; private set; }
        
        protected ViewModelBase vm;
        
        /// <summary>
        /// 是否在激活列表
        /// </summary>
        public bool IsActive { get; private set; }
        
        /// <summary>
        /// 非激活时的毫秒时间戳
        /// </summary>
        public long InactiveAt { get; private set; }
        
        /// <summary>
        /// 背景是否模糊
        /// </summary>
        public bool IsBGBlur { get; protected set; }
        
        /// <summary>
        /// 标记是否是常驻UI
        /// </summary>
        public bool IsPermanent { get; protected set; }
        
        /// <summary>
        /// 是否是顶部
        /// </summary>
        public bool IsTop {
            get
            {
                return _isTop;
            }
            set
            {
                if (_isTop != value)
                {
                    _isTop = value;

                    if (_isTop)
                        OnBeTop();
                    else
                        OnNotTop();
                }
            }
        }

        private bool _isTop;

        public ViewBase(string pkg, string name)
        {
            Pkg = pkg;
            Name = name;
            CustomKey = ViewMgr.GetCustomKey(pkg, name);

            InactiveAt = 0;
            IsActive = true;

            onCreated = DoInit;

            sortingOrder = (int) EViewLayer.Normal;
            fairyBatching = true;
        }

        protected void DoInit()
        {
            InitViewModel();
            
            InitChildren();
            
            OnInit();
        }

        public void DoOpen(ViewArgsBase args = null)
        {
            vm.Init(this, args);

            if (IsBGBlur)
                ViewMgr.Ins.CallBlur(CustomKey, true);

            PlayEnterAnim();
            
            OnOpen(args);
        }

        void PlayEnterAnim()
        {
            var t = GetTransition("transitionCommonEnter");
            if (t == null)
                return;
            
            t.Play();
        }

        public void DoClose()
        {
            vm.Close();
            
            OnCloseChildren();
            
            OnClose();
            
            if (IsBGBlur)
                ViewMgr.Ins.CallBlur(CustomKey, false);
        }

        public void DoDispose()
        {
            vm.Dispose();

            OnDisposeChildren();
            
            OnDispose();
            
            Dispose();
        }
        
        /// <summary>
        /// 激活
        /// </summary>
        public void Active()
        {
            IsActive = true;
            InactiveAt = 0;
        }

        /// <summary>
        /// 非激活
        /// </summary>
        public void Inactive()
        {
            IsActive = false;
            InactiveAt = TimeUtilBase.GetTimestampMS();
        }

        /// <summary>
        /// 是否已经过期
        /// </summary>
        /// <param name="durationMS">过期所需毫秒</param>
        /// <returns></returns>
        public bool IsExpired(long durationMS)
        {
            var leftMS = TimeUtilBase.LeftMS(InactiveAt + durationMS);

            return leftMS <= 0;
        }

        protected abstract void OnInit();
        
        protected abstract void OnOpen(ViewArgsBase args = null);
        
        protected abstract void OnClose();
        
        protected abstract void OnDispose();

        protected abstract void InitChildren();
        
        protected abstract void OnCloseChildren();
        
        protected abstract void OnDisposeChildren();
        
        protected abstract void InitViewModel();

        /// <summary>
        /// 成为顶层回调
        /// </summary>
        protected virtual void OnBeTop()
        {
        }

        /// <summary>
        /// 取消顶层回调
        /// </summary>
        protected virtual void OnNotTop()
        {
        }

        /// <summary>
        /// 关闭自身
        /// </summary>
        protected void CloseSelf()
        {
            ViewMgr.Ins.Close(CustomKey);
        }
    }
}