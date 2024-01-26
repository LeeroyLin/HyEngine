using System.Collections.Generic;
using UnityEngine;

namespace Engine.Scripts.Runtime.FSM
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">用户数据</typeparam>
    public abstract class FSMBase<T> : IFSM, IFSMParameterOp where T : class
    {
        public IFSMState CurrState { get; private set; }
        
        private Dictionary<string, FSMParameter> _parameterDic = new Dictionary<string, FSMParameter>();
        private Dictionary<int, IFSMState> _stateDic = new Dictionary<int, IFSMState>();

        protected T userData;

        public FSMBase(T userData, int initStateId)
        {
            this.userData = userData;

            Change2State(initStateId);
        }

        public void DoUpdate()
        {
            OnBeforeUpdate();
            
            CurrState?.DoUpdate();
            
            OnAfterUpdate();
        }

        public void DoLateUpdate()
        {
            OnBeforeLateUpdate();

            CurrState?.DoLateUpdate();
            
            OnAfterLateUpdate();
        }

        public void DoFixedUpdate()
        {
            OnBeforeFixedUpdate();

            CurrState?.DoFixedUpdate();
            
            OnAfterFixedUpdate();
        }

        public void AddParameter(string name, FSMParameter param)
        {
            _parameterDic.Add(name, param);
        }

        public void RemoveParameter(string name, FSMParameter param)
        {
            _parameterDic.Remove(name);
        }

        public void ClearParameters()
        {
            _parameterDic.Clear();
        }

        public FSMParameter GetParameter(string name)
        {
            _parameterDic.TryGetValue(name, out var param);
            return param;
        }

        public void SetIntParameter(string name, int val, int fromStateId = -1)
        {
            if (!_parameterDic.TryGetValue(name, out var parameter))
            {
                Error($"can not find parameter '{name}'");
                return;
            }

            if (parameter.Type != EFSMParameterType.Int)
            {
                Error($"wrong parameter type '{name}' use int");
                return;
            }

            parameter.ValInt = val;
            
            CurrState?.OnParameterChanged(name);
        }

        public void SetFloatParameter(string name, float val, int fromStateId = -1)
        {
            if (!_parameterDic.TryGetValue(name, out var parameter))
            {
                Error($"can not find parameter '{name}'");
                return;
            }

            if (parameter.Type != EFSMParameterType.Float)
            {
                Error($"wrong parameter type '{name}' use float");
                return;
            }

            parameter.ValFloat = val;
                        
            CurrState?.OnParameterChanged(name);
        }

        public void SetBoolParameter(string name, bool val, int fromStateId = -1)
        {
            if (!_parameterDic.TryGetValue(name, out var parameter))
            {
                Error($"can not find parameter '{name}'");
                return;
            }

            if (parameter.Type != EFSMParameterType.Bool)
            {
                Error($"wrong parameter type '{name}' use bool");
                return;
            }

            parameter.ValBool = val;
                        
            CurrState?.OnParameterChanged(name);
        }

        protected abstract void OnBeforeUpdate();
        protected abstract void OnAfterUpdate();
        protected abstract void OnBeforeLateUpdate();
        protected abstract void OnAfterLateUpdate();
        protected abstract void OnBeforeFixedUpdate();
        protected abstract void OnAfterFixedUpdate();
        protected abstract FSMStateBase<T> OnCreateState(int stateId);

        protected K CreateState<K>(int stateId) where K : FSMStateBase<T>, new()
        {
            var k = new K();
            k.InitData(stateId, userData, this, OnTranslate);
            return k;
        }

        protected void OnTranslate(int stateId)
        {
            Change2State(stateId);
        }

        void Change2State(int stateId)
        {
            if (!_stateDic.TryGetValue(stateId, out var state))
            {
                state = OnCreateState(stateId);
                _stateDic.Add(stateId, state);
            }

            CurrState?.DoExit();

            CurrState = state;
            
            CurrState.DoEnter();
        }

        void Log(string msg)
        {
            Debug.Log($"【FSM】 {msg}");
        }

        void Error(string msg)
        {
            Debug.LogError($"【FSM】 {msg}");
        }
    }
}