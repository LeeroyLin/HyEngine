using System.Collections.Generic;
using Engine.Scripts.Runtime.Language;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Utils;
using UnityEngine;

namespace Engine.Scripts.Runtime.Cfg
{
    public class CfgMgr : SingletonClass<CfgMgr>, IManager
    {
        private Dictionary<string, CfgI18nBase> _i18nCfgDic = new Dictionary<string, CfgI18nBase>();

        private LogGroup _log;
        
        public void Reset()
        {
        }

        public void Init(ICfgI18nGenerator generator)
        {
            _log = new LogGroup("CfgMgr");
            
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
        /// <param name="args"></param>
        /// <returns></returns>
        public string GetI18nVal(string key, params string[] args)
        {
            if (!GetI18nCfgNameByKey(key, out var cfgName))
            {
                _log.Warning($"Wrong i18n key '{key}'");
                
                return key;
            }

            if (!_i18nCfgDic.TryGetValue(cfgName, out var cfg))
            {
                _log.Warning($"Can not find i18n cfg '{cfgName}', key '{key}'");
                
                return key;
            }

            var content = cfg.GetByKey(key, LanguageMgr.Ins.LangStr);

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                
                content = content.Replace($"{{{i}}}", arg);
            }

            content = content.Replace("<br>", "\n");
            content = content.Replace("[color=", "[color=#");
            
            return content;
        }

        /// <summary>
        /// 是否有多语言键
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasI18nKey(string key)
        {
            if (!GetI18nCfgNameByKey(key, out var cfgName))
                return false;

            if (!_i18nCfgDic.TryGetValue(cfgName, out var cfg))
                return false;

            if (!cfg.HasKey(key))
                return false;

            return true;
        }

        bool GetI18nCfgNameByKey(string key, out string cfgName)
        {            
            var strs = key.Split("_");

            if (strs.Length > 2)
            {
                cfgName = strs[0] + "_" + strs[1];
                return true;
            }

            cfgName = "";

            return false;
        }
    }
}