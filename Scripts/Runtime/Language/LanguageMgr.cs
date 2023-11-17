using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Utils;

namespace Engine.Scripts.Runtime.Language
{
    public class LanguageMgr : SingletonClass<LanguageMgr>, IManager
    {
        public string LangStr { get; private set; }
        
        public void Reset()
        {
        }
        
        public void Init()
        {
            LangStr = "zh-cn";
        }
    }
}