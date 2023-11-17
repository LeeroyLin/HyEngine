using System.Collections.Generic;

namespace Engine.Scripts.Runtime.Cfg
{
    public abstract class CfgI18nBase
    {
        public string CfgName { get; private set; }
        
        // 语言名对应下标字典
        Dictionary<string, int> _langIdxDic;

        // 记录所有数据，键为数据键名，值为不同语言的值，下标与_langIdxDic一致
        Dictionary<string, string[]> _dataDic;

        public CfgI18nBase(string cfgName)
        {
            CfgName = cfgName;
            
            _langIdxDic = OnGetLangIdxDic();
            _dataDic = OnGetDataDic();
        }
        
        protected abstract Dictionary<string, int> OnGetLangIdxDic();
        protected abstract Dictionary<string, string[]> OnGetDataDic();
        
        public string GetByKey(string key, string langStr)
        {
            if (!_langIdxDic.TryGetValue(key, out var idx))
                return key;
            
            if (!_dataDic.TryGetValue(langStr, out var dataList))
                return key;

            return idx < dataList.Length ? dataList[idx] : key;
        }
    }
}