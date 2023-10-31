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

            var content = $"【{Title}】 {msg} {args}";
            LogMgr.Ins.Log(content);
        }

        public void Error(string msg, params object[] args)
        {
            if (!IsEnabled)
                return;
            
            var content = $"【{Title}】 {msg} {args}";
            LogMgr.Ins.LogError(content);
        }

        public void Warning(string msg, params object[] args)
        {
            if (!IsEnabled)
                return;
            
            var content = $"【{Title}】 {msg} {args}";
            LogMgr.Ins.LogWarning(content);
        }
    }
}