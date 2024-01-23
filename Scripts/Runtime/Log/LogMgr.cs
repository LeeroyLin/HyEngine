using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Utils;
using UnityEngine;

namespace Engine.Scripts.Runtime.Log
{
    public class LogMgr : SingletonClass<LogMgr>, IManager
    {
        private LogConfig _config;
        
        public void Reset()
        {
        }

        public void Init(LogConfig config)
        {
            _config = config;
            
            // 创建文件目录
            TryCreateDir();
        }

        /// <summary>
        /// 打印日志
        /// </summary>
        /// <param name="content">内容</param>
        public void Log(string content)
        {
            if (_config == null)
                return;
            
            TryLog(content);
            TrySaveLog(content);
        }
        
        /// <summary>
        /// 打印警告
        /// </summary>
        /// <param name="content">内容</param>
        public void LogWarning(string content)
        {
            if (_config == null)
                return;
            
            TryWarning(content);
            TrySaveWarning(content);
        }
        
        /// <summary>
        /// 打印错误
        /// </summary>
        /// <param name="content">内容</param>
        public void LogError(string content)
        {
            if (_config == null)
                return;
            
            TryError(content);
            TrySaveError(content);
        }

        private void TryLog(string content)
        {
            if (!_config.isShowLog)
                return;
               
            Debug.Log(content);
        }
        
        private void TryWarning(string content)
        {
            if (!_config.isShowWarning)
                return;
            
            Debug.LogWarning(content);
        }
        
        private void TryError(string content)
        {
            if (!_config.isShowError)
                return;
            
            Debug.LogError(content);
        }
        
        private void TrySaveLog(string content)
        {
            if (!_config.isSaveLog)
                return;

            // todo
        }
        
        private void TrySaveWarning(string content)
        {
            if (!_config.isSaveWarning)
                return;

            // todo
        }
        
        private void TrySaveError(string content)
        {
            if (!_config.isSaveError)
                return;

            // todo
        }

        // 尝试创建文件目录
        private void TryCreateDir()
        {
            if (!_config.isSaveLog && !_config.isSaveWarning && !_config.isSaveError)
                return;
            
            // todo
        }
    }
}