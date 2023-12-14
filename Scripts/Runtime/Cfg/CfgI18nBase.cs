using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;

namespace Engine.Scripts.Runtime.Cfg
{
    public abstract class CfgI18nBase
    {
        public string CfgName { get; private set; }
        
        // 语言名对应下标字典
        Dictionary<string, int> _langIdxDic;

        // 记录所有数据，键为数据键名，值为不同语言的值，下标与_langIdxDic一致
        Dictionary<string, string[]> _dataDic;

        private LogGroup _log;
        
        public CfgI18nBase(string cfgName)
        {
            _log = new LogGroup(cfgName);
            
            CfgName = cfgName;
            
            _langIdxDic = OnGetLangIdxDic();
            _dataDic = OnGetDataDic();
        }
        
        protected abstract Dictionary<string, int> OnGetLangIdxDic();
        protected abstract Dictionary<string, string[]> OnGetDataDic();
        
        public string GetByKey(string key, string langStr)
        {
            if (!_langIdxDic.TryGetValue(langStr, out var idx))
            {
                _log.Warning($"Can not find i18n lang ''{langStr}' field in cfg '{CfgName}'");
                
                return key;
            }

            if (!_dataDic.TryGetValue(key, out var dataList))
            {
                _log.Warning($"Can not find i18n key '{key}' in cfg '{CfgName}'.");
                
                return key;
            }

            return idx < dataList.Length ? dataList[idx] : key;
        }

        public bool HasKey(string key)
        {
            return _dataDic.ContainsKey(key);
        }
    }
}