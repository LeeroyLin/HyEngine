using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.Timer;
using Engine.Scripts.Runtime.Utils;
using FairyGUI;
using UnityEngine;

namespace Engine.Scripts.Runtime.View
{
    public class ViewMgr : SingletonClass<ViewMgr>, IManager
    {
        // 界面非激活后的销毁时间
        static readonly long EXPIRE_TIME_MS = 60000;
        
        // 当前激活的UI列表
        private List<ViewBase> _activeUIList = new List<ViewBase>();
        
        // 非激活的UI列表，UI等待被销毁
        private List<ViewBase> _inactiveUIList = new List<ViewBase>();

        private IViewExtension _viewExtension;
        
        private LogGroup _log;
        
        public void Reset()
        {
        }
        
        /// <summary>
        /// 获得自定义UI键名
        /// </summary>
        /// <param name="pkg">UI包名</param>
        /// <param name="name">UI名</param>
        /// <returns></returns>
        public static string GetCustomKey(string pkg, string name)
        {
            return $"{pkg}_{name}";
        }

        /// <summary>
        /// 通过自定义UI键名，获得包名和UI名
        /// </summary>
        /// <param name="customKey"></param>
        /// <returns></returns>
        public static (string, string) CustomKey2PkgAndName(string customKey)
        {
            var strs = customKey.Split("_");
            return (strs[0], strs[1]);
        }

        public void Init(IViewExtension viewExtension)
        {
            _log = new LogGroup("ViewMgr");
            
            // 注册界面扩展
            var extensions = viewExtension.InitViewExtension();
            foreach (var extension in extensions)
            {
                UIObjectFactory.SetPackageItemExtension(extension.Url, extension.Creator);
            }
            
            // 注册计时器
            TimerMgr.Ins.UseLateUpdate(OnTimer);
        }

        /// <summary>
        /// 打开界面
        /// </summary>
        /// <param name="key">界面键</param>
        /// <param name="args">参数，可空</param>
        public void Open(string key, ViewArgsBase args = null)
        {
            ViewBase ins = null;
            
            // 是否在非激活状态
            if (FindViewIdx(_inactiveUIList, key, out var idx))
            {
                // 获得信息
                ins = _inactiveUIList[idx];
                
                // 从非激活列表移除
                _inactiveUIList.RemoveAt(idx);
                
                // 标记激活
                ins.Active();
                
                // 显示节点
                ins.visible = true;
            }
            // 是否已经激活
            else if (FindViewIdx(_activeUIList, key, out var i))
            {
                // 关闭该界面
                CloseAt(i);
            }
            
            // 新建实例
            if (ins == null)
            {
                // 新建UI
                var (pkgName, uiName) = CustomKey2PkgAndName(key);
                ins = ResMgr.Ins.CreateUIObject(pkgName, uiName) as ViewBase;

                if (ins == null)
                {
                    _log.Error($"Load view '{pkgName}_{uiName}' failed.");
                    return;
                }
                
                ins.name = $"{ins.Pkg}_{ins.Name}";
                ins.MakeFullScreen();
            }
            else
            {
                GRoot.inst.RemoveChild(ins, false);
            }
            
            GRoot.inst.AddChild(ins);

            _activeUIList.Add(ins);
            
            // 回调
            ins.DoOpen(args);
            
            // LogList();
        }

        /// <summary>
        /// 关闭界面
        /// </summary>
        /// <param name="key">界面键</param>
        public void Close(string key)
        {
            // 如果没有该UI
            if (!FindViewIdx(_activeUIList, key, out var idx))
            {
                _log.Warning("[Close] Can not find ui with key:'{0}' in current ui list.", key);
            
                return;
            }

            CloseAt(idx);
            
            // LogList();
        }

        /// <summary>
        /// 按下标关闭界面
        /// </summary>
        /// <param name="idx">界面在激活列表中的下标</param>
        public void CloseAt(int idx)
        {
            var info = _activeUIList[idx];
            
            // 从激活列表移除
            _activeUIList.RemoveAt(idx);

            // 标记非激活
            info.Inactive();
            
            // 隐藏
            info.visible = false;
            
            // 回调
            info.DoClose();

            // 放入非激活列表
            _inactiveUIList.Add(info);
        }

        /// <summary>
        /// 释放界面
        /// </summary>
        /// <param name="idx">界面在非激活列表中的下标</param>
        public void DisposeAt(int idx)
        {
            var info = _inactiveUIList[idx];
            
            // 销毁
            info.DoDispose();
            
            // 移除数据
            _inactiveUIList.RemoveAt(idx);
        }
        
        /// <summary>
        /// 从当前UI列表中获得对应UI下标
        /// 未查找到返回-1
        /// </summary>
        /// <param name="list"></param>
        /// <param name="key"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        private bool FindViewIdx(List<ViewBase> list, string key, out int idx)
        {
            idx = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].CustomKey == key)
                {
                    idx = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 关闭对应下标及之后的所有UI
        /// </summary>
        /// <param name="idx"></param>
        private void CloseAfter(int idx)
        {
            for (int i = _activeUIList.Count - 1; i >= idx; i--)
                CloseAt(i);
        }
        
        /// <summary>
        /// 计时器回调
        /// </summary>
        void OnTimer()
        {
            // 遍历
            for (int i = 0; i < _inactiveUIList.Count; i++)
            {
                var info = _inactiveUIList[i];
                
                // 是否过期了
                if (info.IsExpired(EXPIRE_TIME_MS))
                {
                    // 销毁该UI
                    DisposeAt(i);
                }
                else
                {
                    // 因为非激活界面从后插入，正向遍历到没过期的项，该项之后都不会过期。
                    return;
                }
            }
        }

        void LogList()
        {
            _log.Log("============");
            _log.Log("ActiveUIList:");
            
            foreach (var view in _activeUIList)
            {
                _log.Log(view.CustomKey);
            }
            
            _log.Log("InactiveUIList:");
            foreach (var view in _inactiveUIList)
            {
                _log.Log(view.CustomKey);
            }
        }
    }
}