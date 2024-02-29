using System;
using System.Collections.Generic;
using System.Text;
using Engine.Scripts.Runtime.ClassPool;
using UnityEngine;

namespace Engine.Scripts.Runtime.Utils.QuadTree
{
    /// <summary>
    /// 四叉树
    /// </summary>
    public partial class QuadTree<T>
    {
        public QuadTreeNode<T> RootNode { get; private set; }

        private int _divideChildrenNum;
        private int _maxTreeLayer;
        private Func<T, Vector2> _getPosHandler;
        private Func<T, T, bool> _compareHandler;

        private StringBuilder _sb;
        
        public QuadTree(Vector2 ltPos, Vector2 rbPos, int divideChildrenNum, int maxTreeLayer, 
            Func<T, Vector2> getPosHandler, Func<T, T, bool> compareHandler)
        {
            _divideChildrenNum = divideChildrenNum;
            _maxTreeLayer = maxTreeLayer;
            _getPosHandler = getPosHandler;
            _compareHandler = compareHandler;

            InitClassPool();
            
            RootNode = ClassPoolMgr.Ins.GetIns<QuadTreeNode<T>, QuadTreeNodeInitData>(new QuadTreeNodeInitData(ltPos, rbPos));
        }

        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="data"></param>
        public void Add(T data)
        {
            AddData2Node(RootNode, 1, data);
        }

        /// <summary>
        /// 移除数据
        /// </summary>
        /// <param name="data"></param>
        public void Remove(T data)
        {
            var node = RootNode;
            
            var pos = _getPosHandler(data);
            
            while (true)
            {
                var side = GetSide(pos, node.centerPos);
                if (!node.sideDic.TryGetValue(side, out var sideData))
                    break;

                if (sideData.child != null)
                {
                    node = sideData.child;
                    continue;
                }

                if (sideData.datas == null)
                    return;

                for (int i = 0; i < sideData.datas.Count; i++)
                {
                    var rData = sideData.datas[i];

                    if (_compareHandler(rData, data))
                    {
                        sideData.datas.RemoveAt(i);

                        if (sideData.datas.Count == 0)
                        {
                            node.sideDic.Remove(side);
                            
                            var child = sideData.child;
                            ClassPoolMgr.Ins.SetIns<EachSide<T>, object>(sideData);
                            ClassPoolMgr.Ins.SetIns<QuadTreeNode<T>, QuadTreeNodeInitData>(child);
                        }
                        
                        return;
                    }
                }

                break;
            }
        }
        
        /// <summary>
        /// 获得矩形范围内所有的数据
        /// </summary>
        /// <param name="ltPos"></param>
        /// <param name="rbPos"></param>
        /// <returns></returns>
        public List<T> GetAllInRect(Vector2 ltPos, Vector2 rbPos)
        {
            List<T> list = new List<T>();
            
            CheckNodeInRect(ltPos, rbPos, RootNode, list);
            
            return list;
        }
        
        /// <summary>
        /// 获得矩形范围内所有的数据
        /// </summary>
        /// <param name="ltPos"></param>
        /// <param name="rbPos"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public void GetAllInRectWithList(Vector2 ltPos, Vector2 rbPos, List<T> list)
        {
            list.Clear();
            
            CheckNodeInRect(ltPos, rbPos, RootNode, list);
        }
        
        public override string ToString()
        {
            if (_sb == null)
                _sb = new StringBuilder();
            
            _sb.AppendLine("【QuadTree】 Log start.");
            LogNode(RootNode, 1, "");
            _sb.AppendLine("【QuadTree】 Log end.");

            return _sb.ToString();
        }

        void CheckNodeInRect(Vector2 ltPos, Vector2 rbPos, QuadTreeNode<T> node, List<T> list)
        {
            // 矩形超出左范围
            if (rbPos.x <= node.ltPos.x)
                return;

            // 矩形超出右范围
            if (ltPos.x >= node.rbPos.x)
                return;

            // 矩形超出上范围
            if (rbPos.y >= node.ltPos.y)
                return;

            // 矩形超出下范围
            if (ltPos.y <= node.rbPos.y)
                return;
            
            // 矩形在中心上
            bool isTopFromCenter = rbPos.y >= node.centerPos.y;
            
            // 矩形在中心下
            bool isBottomFromCenter = ltPos.y < node.centerPos.y;

            // 矩形在中心左
            bool isLeftFromCenter = rbPos.x <= node.centerPos.x;

            // 矩形在中心右
            bool isRightFromCenter = ltPos.x > node.centerPos.x;

            if (!isRightFromCenter && !isBottomFromCenter)
                CheckSideInRect(ltPos, rbPos, node, ESide.LT, list);

            if (!isLeftFromCenter && !isBottomFromCenter)
                CheckSideInRect(ltPos, rbPos, node, ESide.RT, list);

            if (!isRightFromCenter && !isTopFromCenter)
                CheckSideInRect(ltPos, rbPos, node, ESide.LB, list);

            if (!isLeftFromCenter && !isTopFromCenter)
                CheckSideInRect(ltPos, rbPos, node, ESide.RB, list);
        }

        void CheckSideInRect(Vector2 ltPos, Vector2 rbPos, QuadTreeNode<T> node, ESide side, List<T> list)
        {
            if (!node.sideDic.TryGetValue(side, out var sideData))
                return;
            
            if (sideData.child != null)
            {
                CheckNodeInRect(ltPos, rbPos, sideData.child, list);
            }
            else
            {
                if (MathUtil.IsRectCross(node.ltPos, node.rbPos, ltPos, rbPos))
                    foreach (var data in sideData.datas)
                        list.Add(data);
            }
        }

        void AddData2Node(QuadTreeNode<T> node, int layer, T data)
        {
            var pos = _getPosHandler(data);
            
            var side = GetSide(pos, node.centerPos);

            // 初始化方向信息
            if (!node.sideDic.TryGetValue(side, out var sideData))
            {
                sideData = ClassPoolMgr.Ins.GetIns<EachSide<T>, object>(null);
                
                node.sideDic.Add(side, sideData);
            }
            
            // 是否有子节点
            if (sideData.child != null)
            {
                AddData2Node(sideData.child, layer + 1, data);
            }
            else
            {
                // 初始化数据
                if (sideData.datas == null)
                    sideData.datas = new List<T>();
                    
                // 层没满，数据数量满足分裂数量
                if (layer < _maxTreeLayer && sideData.datas.Count >= _divideChildrenNum)
                {
                    sideData.datas.Add(data);
                        
                    // 创建新层
                    NewLayer(node, side, layer);
                }
                else
                    sideData.datas.Add(data);
            }
        }

        void NewLayer(QuadTreeNode<T> node, ESide side, int layer)
        {
            Vector2 ltPos = Vector2.zero;
            Vector2 rbPos = Vector2.zero;

            switch (side)
            {
                case ESide.LT:
                    ltPos = node.ltPos;
                    rbPos = node.centerPos;
                    break;
                case ESide.RT:
                    ltPos.x = node.centerPos.x;
                    ltPos.y = node.ltPos.y;
                    rbPos.x = node.rbPos.x;
                    rbPos.y = node.centerPos.y;
                    break;
                case ESide.LB:
                    ltPos.x = node.ltPos.x;
                    ltPos.y = node.centerPos.y;
                    rbPos.x = node.centerPos.x;
                    rbPos.y = node.rbPos.y;
                    break;
                case ESide.RB:
                    ltPos = node.centerPos;
                    rbPos = node.rbPos;
                    break;
            }
            
            var sideData = node.sideDic[side];
            sideData.child = ClassPoolMgr.Ins.GetIns<QuadTreeNode<T>, QuadTreeNodeInitData>(new QuadTreeNodeInitData(ltPos, rbPos));

            foreach (var data in sideData.datas)
                AddData2Node(sideData.child, layer + 1, data);

            sideData.datas = null;
        }

        ESide GetSide(Vector2 targetPos, Vector2 centerPos)
        {
            if (targetPos.x <= centerPos.x)
            {
                if (targetPos.y <= centerPos.y)
                    return ESide.LB;
                
                return ESide.LT;
            }
            
            if (targetPos.y <= centerPos.y)
                return ESide.RB;
            
            return ESide.RT;
        }
        
        void LogNode(QuadTreeNode<T> node, int layer, string preStr)
        {
            foreach (var info in node.sideDic)
            {
                for (int i = 0; i < layer - 1; i++)
                    _sb.Append("\t");
                
                _sb.AppendLine($"{info.Key}");
                
                var newPreStr = $"{preStr} {info.Key}{layer}";
                
                if (info.Value.child != null)
                {
                    LogNode(info.Value.child, layer + 1, newPreStr);
                    continue;
                }

                if (info.Value.datas != null)
                {
                    foreach (var data in info.Value.datas)
                    {
                        for (int i = 0; i < layer; i++)
                            _sb.Append("\t");
                        
                        _sb.AppendLine(data.ToString());
                    }   
                }
            }
        }
    }
}