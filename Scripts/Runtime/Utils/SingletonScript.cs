using UnityEngine;

namespace Engine.Scripts.Runtime.Utils
{
    public class SingletonScript<T> : MonoBehaviour where T : class
    {
        public static T Ins
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType(typeof(T)) as T;
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        private static T _instance;
    } 
}