namespace Engine.Scripts.Runtime.Utils
{
    public class SingletonClass<T> where T:class, new()
    {
        public static T Ins
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
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