using Engine.Scripts.Runtime.Manager;

namespace Engine.Scripts.Runtime.Language
{
    public class LanguageMgr : ManagerBase<LanguageMgr>
    {
        public string LangStr { get; private set; }
        
        public void Init()
        {
            LangStr = "zh-cn";
        }
        
        protected override void OnReset()
        {
        }

        protected override void OnDisposed()
        {
        }
    }
}