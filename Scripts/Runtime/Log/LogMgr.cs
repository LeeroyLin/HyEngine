using Client.Scripts.Runtime.Global;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Utils;
using UnityEngine;

namespace Engine.Scripts.Runtime.Log
{
    public class LogMgr : SingletonClass<LogMgr>, IManager
    {
        public void Reset()
        {
        }

        public void Init()
        {
            // 创建文件目录
            TryCreateDir();
        }

        /// <summary>
        /// 打印日志
        /// </summary>
        /// <param name="content">内容</param>
        public void Log(string content)
        {
            TryLog(content);
            TrySaveLog(content);
        }
        
        /// <summary>
        /// 打印警告
        /// </summary>
        /// <param name="content">内容</param>
        public void LogWarning(string content)
        {
            TryWarning(content);
            TrySaveWarning(content);
        }
        
        /// <summary>
        /// 打印错误
        /// </summary>
        /// <param name="content">内容</param>
        public void LogError(string content)
        {
            TryError(content);
            TrySaveError(content);
        }

        private void TryLog(string content)
        {
            if (!GlobalConfig.LogConfig.isShowLog)
                return;
            
            Debug.Log(content);
        }
        
        private void TryWarning(string content)
        {
            if (!GlobalConfig.LogConfig.isShowWarning)
                return;
            
            Debug.LogWarning(content);
        }
        
        private void TryError(string content)
        {
            if (!GlobalConfig.LogConfig.isShowError)
                return;
            
            Debug.LogError(content);
        }
        
        private void TrySaveLog(string content)
        {
            if (!GlobalConfig.LogConfig.isSaveLog)
                return;

            // todo
        }
        
        private void TrySaveWarning(string content)
        {
            if (!GlobalConfig.LogConfig.isSaveWarning)
                return;

            // todo
        }
        
        private void TrySaveError(string content)
        {
            if (!GlobalConfig.LogConfig.isSaveError)
                return;

            // todo
        }

        // 尝试创建文件目录
        private void TryCreateDir()
        {
            if (!GlobalConfig.LogConfig.isSaveLog && !GlobalConfig.LogConfig.isSaveWarning && !GlobalConfig.LogConfig.isSaveError)
                return;
            
            // todo
        }
    }
}