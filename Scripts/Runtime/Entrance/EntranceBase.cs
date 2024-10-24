﻿using Engine.Scripts.Runtime.Utils;
using EngineRuntimeAOT;
using UnityEngine;

namespace Engine.Scripts.Runtime.Entrance
{
    public abstract class EntranceBase : IEntrance
    {
        protected object[] Args;
        
        public void DoStart(MonoBehaviour behaviour, params object[] args)
        {
            MonoHelper.Ins.Behaviour = behaviour;
            Args = args;
            
            OnInit();
            OnStart();
        }

        public void DoUpdate()
        {
            OnUpdate();
        }

        public void DoDispose()
        {
            OnDispose();
        }

        public void DoLateUpdate()
        {
            OnLateUpdate();
        }
        
        public void DoFixedUpdate()
        {
            OnFixedUpdate();
        }

        protected abstract void OnInit();
        protected abstract void OnStart();
        protected abstract void OnDispose();
        protected abstract void OnUpdate();
        protected abstract void OnLateUpdate();
        protected abstract void OnFixedUpdate();
    }
}