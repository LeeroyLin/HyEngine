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
            
            if (args.Length > 0)
                content = $"【{Title}】 {string.Format(msg, args)}";
            else
                content = $"【{Title}】 {msg}";
            
            LogMgr.Ins.Log(content);
        }

        public void Error(string msg, params object[] args)
        {
            if (!IsEnabled)
                return;
            
            if (args.Length > 0)
            {
                var content = $"【{Title}】 {string.Format(msg, args)}";
                LogMgr.Ins.LogError(content);
                return;
            }
            
            LogMgr.Ins.LogError(msg);
        }

        public void Warning(string msg, params object[] args)
        {
            if (!IsEnabled)
                return;
            
            if (args.Length > 0)
            {
                var content = $"【{Title}】 {string.Format(msg, args)}";
                LogMgr.Ins.LogWarning(content);
                return;
            }
            
            LogMgr.Ins.LogWarning(msg);
        }
    }
}