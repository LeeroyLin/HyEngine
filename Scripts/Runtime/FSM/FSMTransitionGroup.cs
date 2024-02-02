using System;
using System.Collections.Generic;

namespace Engine.Scripts.Runtime.FSM
{
    public class FSMTransitionGroup
    {
        public int NextStateId { get; private set; }
        private Dictionary<string, FSMTransition> _transitionDic = new Dictionary<string, FSMTransition>();

        public FSMTransitionGroup(FSMTransition[] transitions, int nextStateId)
        {
            foreach (var transition in transitions)
                _transitionDic.Add(transition.ParameterName, transition);
            
            NextStateId = nextStateId;
        }

        public bool Contains(string parameterName)
        {
            return _transitionDic.ContainsKey(parameterName);
        }

        public void Foreach(Action<FSMTransition> handler)
        {
            foreach (var transition in _transitionDic)
                handler(transition.Value);
        }
    }
}