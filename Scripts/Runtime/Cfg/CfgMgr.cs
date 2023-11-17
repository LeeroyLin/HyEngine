using System.Collections.Generic;
using Engine.Scripts.Runtime.Language;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Utils;

namespace Engine.Scripts.Runtime.Cfg
{
    public class CfgMgr : SingletonClass<CfgMgr>, IManager
    {
        private Dictionary<string, CfgI18nBase> _i18nCfgDic = new Dictionary<string, CfgI18nBase>();

        public void Reset()
        {
        }

        public void Init(ICfgI18nGenerator generator)
        {
            var arr = generator.GetCfgI18nArr();
            foreach (var data in arr)
            {
                _i18nCfgDic.Add(data.CfgName, data);
            }
        }

        /// <summary>
        /// 通过多语言键名获得多语言文本
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetI18nVal(string key)
        {
            if (!GetI18nCfgNameByKey(key, out var cfgName))
                return "";

            if (!_i18nCfgDic.TryGetValue(cfgName, out var cfg))
                return "";

            return cfg.GetByKey(key, LanguageMgr.Ins.LangStr);
        }

        bool GetI18nCfgNameByKey(string key, out string cfgName)
        {            
            var strs = key.Split("_");

            if (strs.Length > 2)
            {
                cfgName = strs[0] + strs[1];
                return true;
            }

            cfgName = "";

            return false;
        }
    }
}