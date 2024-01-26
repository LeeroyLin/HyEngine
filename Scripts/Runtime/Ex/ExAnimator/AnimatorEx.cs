using System;
using UnityEngine;

namespace Engine.Scripts.Runtime.Ex.ExAnimator
{
    public static class AnimatorEx
    {
        public static void Play(this Animator anim, string animName, Action callback)
        {
            var data = anim.gameObject.GetComponent<AnimatorExData>();
            if (data == null)
                data = anim.gameObject.AddComponent<AnimatorExData>();

            data.PlayWithCallback(animName, callback);
        }
    }
}