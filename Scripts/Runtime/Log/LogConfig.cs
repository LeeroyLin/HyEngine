using System;

namespace Engine.Scripts.Runtime.Log
{
    [Serializable]
    public class LogConfig
    {
        public bool isShowLog;
        public bool isShowWarning;
        public bool isShowError;
        public bool isSaveLog;
        public bool isSaveWarning;
        public bool isSaveError;
    }
}