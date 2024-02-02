using System;
using UnityEngine;

namespace Engine.Scripts.Runtime.Ex.ExAnimator
{
    public class AnimatorExData : MonoBehaviour
    {
        private Action _callback;
        private Animator _anim;
        private int _animNameHash;
        private bool _isStart;
        
        private void Awake()
        {
            _anim = GetComponent<Animator>();
        }

        public void PlayWithCallback(string name, Action callback)
        {
            _animNameHash = Animator.StringToHash(name);
            _callback = callback;
            _isStart = false;

            _anim.Play(name);
        }

        private void LateUpdate()
        {
            if (_callback == null)
                return;

            var stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
            
            if (!_isStart)
            {
                if (stateInfo.shortNameHash == _animNameHash)
                    _isStart = true;
            }

            if (!_isStart)
                return;

            if (stateInfo.shortNameHash != _animNameHash || stateInfo.normalizedTime >= 1)
            {
                _callback();
                _callback = null;                
            }
        }
    }
}