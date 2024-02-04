using System;

namespace Engine.Scripts.Runtime.Log
{
    public class LogGroup
    {
        public string Title { get; private set; }
        public bool IsEnabled { get; private set; }
        
        public LogGroup(string title)
        {
            Title = title;
            IsEnabled = true;
        }

        public void Enable()
        {
            IsEnabled = true;
        }

        public void Disable()
        {
            IsEnabled = false;
        }

        public void Log(string msg, params object[] args)
        {
            if (!IsEnabled)
                return;

            string content;

            string time = DateTime.Now.ToString("HH:mm:ss");
            
            if (args.Length > 0)
                content = $"{time} 【{Title}】 {string.Format(msg, args)}";
            else
                content = $"{time} 【{Title}】 {msg}";
            
            LogMgr.Ins.Log(content);
        }

        public void Error(string msg, params object[] args)
        {
            if (!IsEnabled)
                return;

            string content = "";
            
            string time = DateTime.Now.ToString("HH:mm:ss");

            if (args.Length > 0)
                content = $"{time} 【{Title}】 {string.Format(msg, args)}";
            else
                content = $"{time} 【{Title}】 {msg}";

            LogMgr.Ins.LogError(content);
        }

        public void Warning(string msg, params object[] args)
        {
            if (!IsEnabled)
                return;
            
            string content = "";
            
            string time = DateTime.Now.ToString("HH:mm:ss");

            if (args.Length > 0)
                content = $"{time} 【{Title}】 {string.Format(msg, args)}";
            else
                content = $"{time} 【{Title}】 {msg}";
            
            LogMgr.Ins.LogWarning(content);
        }
    }
}