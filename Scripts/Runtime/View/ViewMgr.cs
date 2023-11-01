using System.Collections.Generic;
using Engine.Scripts.Runtime.Log;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Timer;
using Engine.Scripts.Runtime.Utils;
using FairyGUI;

namespace Engine.Scripts.Runtime.View
{
    public class ViewMgr : SingletonClass<ViewMgr>, IManager
    {
        // 界面非激活后的销毁时间
        public static readonly long EXPIRE_TIME_MS = 60000;
        
        // 当前激活的UI列表
        private List<ViewInfo> _activeUIList = new List<ViewInfo>();
        
        // 非激活的UI列表，UI等待被销毁
        private List<ViewInfo> _inactiveUIList = new List<ViewInfo>();

        private IViewGenerator _generator;
        
        private LogGroup _log;
        
        public void Reset()
        {
        }

        public void Init(IViewGenerator generator)
        {
            _log = new LogGroup("ViewMgr");
            
            _generator = generator;
            
            // 注册计时器
            TimerMgr.Ins.UseLoopTimer(0, OnTimer);
        }
        
        /// <summary>
        /// 打开界面
        /// </summary>
        /// <param name="key">界面键</param>
        /// <param name="args">参数，可空</param>
        public void Open(int key, ViewArgsBase args = null)
        {
            ViewBase ins = null;
            ViewInfo info = null;
            
            // 是否在非激活状态
            if (FindViewIdx(_inactiveUIList, key, out var idx))
            {
                // 获得信息
                info = _inactiveUIList[idx];
                
                // 从非激活列表移除
                _inactiveUIList.RemoveAt(idx);
                
                // 标记激活
                info.Active();
                
                // 获得实例
                ins = info.View;
                
                // 显示节点
                ins.Node.visible = false;
            }
            // 是否已经激活
            else if (FindViewIdx(_activeUIList, key, out var i))
            {
                // 关闭该下标及之后的所有UI
                CloseAfter(i);
            }
            
            // 新建实例
            if (ins == null)
            {
                // 新建实例
                ins = _generator.GetUIIns(key);
                
                // 获得节点
                var node = UIPackage.CreateObject("Main", "WinMain").asCom;
                node.name = $"{ins.Pkg}_{ins.Name}";
                node.MakeFullScreen();
                GRoot.inst.AddChild(node);
                
                // 记录
                ins.Node = node;
                info = new ViewInfo(ins);
            }

            _activeUIList.Add(info);
            
            // 回调
            ins.Init();
            ins.Open(args);
        }

        /// <summary>
        /// 关闭界面
        /// </summary>
        /// <param name="key">界面键</param>
        public void Close(int key)
        {
            // 如果没有该UI
            if (!FindViewIdx(_activeUIList, key, out var idx))
            {
                _log.Warning("[Close] Can not find ui with key:'{0}' in current ui list.", key);
            
                return;
            }

            CloseAt(idx);
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
            info.View.Node.visible = false;
            
            // 回调
            info.View.Close();

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
            
            // todo 销毁
            
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
        private bool FindViewIdx(List<ViewInfo> list, int key, out int idx)
        {
            idx = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].View.Key == key)
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
                Close(i);
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
    }
}