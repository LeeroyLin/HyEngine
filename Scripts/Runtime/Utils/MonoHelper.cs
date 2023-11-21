using UnityEngine;

namespace Engine.Scripts.Runtime.Utils
{
    public class MonoHelper : SingletonClass<MonoHelper>
    {
        public MonoBehaviour Behaviour { get; set; }
    }
}