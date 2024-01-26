using System;
using System.Collections.Generic;
using Engine.Scripts.Runtime.Utils;
using UnityEngine;

namespace Engine.Scripts.Runtime.FSM
{
    public abstract class FSMStateBase<T> : IFSMState where T : class
    {
        public int StateId { get; private set; }

        protected List<FSMTransitionGroup> registedTransitionGroup = new List<FSMTransitionGroup>();

        protected IFSMParameterOp parameterOpHandler;
        protected Action<int> translateHandler;

        protected T userData;

        protected long startAt;
        
        public FSMStateBase()
        {
        }
        
        public void InitData(int id, T userData, IFSMParameterOp parameterOpHandler,
            Action<int> translateHandler)
        {
            StateId = id;
            this.userData = userData;
            this.parameterOpHandler = parameterOpHandler;
            this.translateHandler = translateHandler;
        }

        public int GetId()
        {
            return StateId;
        }

        public void DoEnter()
        {
            Debug.Log($"Enter {GetType().Name} state");

            startAt = TimeUtilBase.GetTimestampMS();
            
            OnRegTransition();
            
            OnEnter();
        }

        public void DoExit()
        {
            Debug.Log($"Exit {GetType().Name} state");

            OnExit();
        }

        public void DoUpdate()
        {
            OnUpdate();
        }

        public void DoLateUpdate()
        {
            OnLateUpdate();
        }

        public void DoFixedUpdate()
        {
            OnFixedUpdate();
        }

        public void OnParameterChanged(string parameterName)
        {
            int stateId = 0;
            bool isOk = false;
            
            foreach (var transitionGroup in registedTransitionGroup)
            {
                if (!transitionGroup.Contains(parameterName))
                    continue;

                isOk = CheckGroupCondition(transitionGroup);
                
                if (isOk)
                {
                    stateId = transitionGroup.NextStateId;
                    break;
                }
            }

            if (!isOk)
                return;
            
            translateHandler?.Invoke(stateId);
        }

        bool CheckGroupCondition(FSMTransitionGroup group)
        {
            bool isOk = true;
         
            group.Foreach(transition =>
            {
                if (!isOk)
                    return;
                
                bool isSame = false;
                bool isLess = false;
                bool isGreater = false;

                var nowVal = parameterOpHandler.GetParameter(transition.ParameterName);
                
                if (transition.Type == EFSMConditionType.Int)
                {
                    isSame = nowVal.ValInt == transition.TargetInt;
                    isLess = nowVal.ValInt < transition.TargetInt;
                    isGreater = nowVal.ValInt > transition.TargetInt;
                }
                else if (transition.Type == EFSMConditionType.Float)
                {
                    isSame = Math.Abs(nowVal.ValFloat - transition.TargetFloat) < float.Epsilon;
                    isLess = nowVal.ValFloat < transition.TargetFloat;
                    isGreater = nowVal.ValFloat > transition.TargetFloat;
                }
                else if (transition.Type == EFSMConditionType.Bool)
                {
                    isSame = nowVal.ValBool == transition.TargetBool;
                    isLess = nowVal.ValBool != transition.TargetBool;
                    isGreater = nowVal.ValBool != transition.TargetBool;
                }

                if (!CheckCondition(transition.Compare, isSame, isLess, isGreater))
                    isOk = false;
            });

            return isOk;
        }

        bool CheckCondition(EFSMConditionCompare compare, bool isSame, bool isLess, bool isGreater)
        {
            switch (compare)
            {
                case EFSMConditionCompare.Equal:
                    return isSame;
                case EFSMConditionCompare.NotEqual:
                    return !isSame;
                case EFSMConditionCompare.EqualLess:
                    return isSame || isLess;
                case EFSMConditionCompare.Less:
                    return isLess;
                case EFSMConditionCompare.EqualGreater:
                    return isSame || isGreater;
                case EFSMConditionCompare.Greater:
                    return isGreater;
            }

            return false;
        }

        /// <summary>
        /// 注册转换组，全满足才会执行
        /// </summary>
        /// <param name="transitionGroup"></param>
        protected void RegTransition(FSMTransitionGroup transitionGroup)
        {
            registedTransitionGroup.Add(transitionGroup);
        }

        /// <summary>
        /// 注册转换
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="stateId"></param>
        protected void RegTransition(FSMTransition transition, int stateId)
        {
            registedTransitionGroup.Add(new FSMTransitionGroup(new []{transition}, stateId));
        }

        protected abstract void OnRegTransition();
        protected abstract void OnEnter();
        protected abstract void OnUpdate();
        protected abstract void OnLateUpdate();
        protected abstract void OnFixedUpdate();
        protected abstract void OnExit();
    }
}