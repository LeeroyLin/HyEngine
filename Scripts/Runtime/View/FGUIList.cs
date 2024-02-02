using System;
using FairyGUI;
using UnityEngine;

namespace Engine.Scripts.Runtime.View
{
    public class FGUIList<T> where T : GComponent
    {
        public GList List { get; private set; }
        private Action<int, T> _showCellHandler;
        private Action<int, T> _clickCellHandler;
        private Action<int, GComponent> _showGCompCellHandler;
        private Action<int, GComponent> _clickGCompCellHandler;
        private Func<int, string> _setCellHandler;

        public FGUIList(GObject obj)
        {
            List = obj as GList;
        }
        
        public void Init(bool isVirtual, Action<int, T> showCellHandler, Action<int, T> clickCellHandler = null)
        {
            if (isVirtual)
                List.SetVirtual();

            _showCellHandler = showCellHandler;

            List.itemRenderer = OnShowListCell;

            if (clickCellHandler != null)
            {
                _clickCellHandler = clickCellHandler;
                List.onClickItem.Add(OnClickCell);
            }
        }
        
        public void Init(bool isVirtual, Action<int, GComponent> showCellHandler, Action<int, GComponent> clickCellHandler = null, Func<int, string> setCellHandler = null)
        {
            if (isVirtual)
                List.SetVirtual();

            _showGCompCellHandler = showCellHandler;

            List.itemRenderer = OnShowListCell;

            if (setCellHandler != null)
            {
                _setCellHandler = setCellHandler;
                List.itemProvider = OnSetListCell;
            }

            if (clickCellHandler != null)
            {
                _clickGCompCellHandler = clickCellHandler;
                List.onClickItem.Add(OnClickCell);
            }
        }

        /// <summary>
        /// 设置列表数据数量
        /// <param name="num">数量</param>
        /// <param name="isAdaptWidthByChildren">是否根据子节点适配宽。 FGUI编辑器里，必须禁用 "自动调整列表项目大小"</param> 
        /// <param name="isAdaptHeightByChildren">是否根据子节点适配高。 FGUI编辑器里，必须禁用 "自动调整列表项目大小"</param> 
        /// </summary>
        public void SetNum(int num, bool isAdaptWidthByChildren = false, bool isAdaptHeightByChildren = false)
        {
            List.numItems = num;
            
            if (isAdaptWidthByChildren)
                AdaptWidthByChildren();
            
            if (isAdaptHeightByChildren)
                AdaptHeightByChildren();
        }

        /// <summary>
        /// 根据子节点，适配宽度
        /// FGUI编辑器里，必须禁用 "自动调整列表项目大小"
        /// </summary>
        public void AdaptWidthByChildren()
        {
            if (List.isVirtual)
            {
                List.width = List.scrollPane.contentWidth + List.margin.left + List.margin.right;
            }
            else
            {
                var listChildren = List.GetChildren();
                
                float width = (listChildren.Length - 1) * List.columnGap + List.margin.left + List.margin.right;
                
                foreach (var child in listChildren)
                {
                    width += child.width;
                }

                List.width = width;
            }
        }

        /// <summary>
        /// 根据子节点，适配高度
        /// FGUI编辑器里，必须禁用 "自动调整列表项目大小"
        /// </summary>
        public void AdaptHeightByChildren()
        {
            if (List.isVirtual)
            {
                List.height = List.scrollPane.contentHeight + List.margin.top + List.margin.bottom;
            }
            else
            {
                var listChildren = List.GetChildren();
                
                float height = (listChildren.Length - 1) * List.lineGap + List.margin.top + List.margin.bottom;
                
                foreach (var child in listChildren)
                {
                    height += child.height;
                }

                List.height = height;
            }
        }

        public void RefreshList()
        {
            if (List.isVirtual)
                List.RefreshVirtualList();
            else
            {
                var num = List.numItems;

                if (num == 0)
                    return;
                    
                List.numItems = 0;
                SetNum(num);
            }
        }

        /// <summary>
        /// 设置行数
        /// </summary>
        /// <param name="num"></param>
        public void SetRowNum(int num)
        {
            List.lineCount = num;
        }

        /// <summary>
        /// 设置列数
        /// </summary>
        /// <param name="num"></param>
        public void SetColumNum(int num)
        {
            List.columnCount = num;
        }

        public void ScrollLeft()
        {
            List.scrollPane.ScrollLeft();
        }

        public void ScrollRight()
        {
            List.scrollPane.ScrollRight();
        }

        public void ScrollTop()
        {
            List.scrollPane.ScrollTop();
        }

        public void ScrollBottom()
        {
            List.scrollPane.ScrollBottom();
        }

        public void ScrollToView(int index, bool isAnim = false)
        {
            List.ScrollToView(index, isAnim);
        }

        void OnShowListCell(int idx, GObject obj)
        {
            _showGCompCellHandler?.Invoke(idx, obj as GComponent);
            _showCellHandler?.Invoke(idx, obj as T);
        }

        string OnSetListCell(int idx)
        {
            return _setCellHandler(idx);
        }

        void OnClickCell(EventContext ctx)
        {
            var obj = ctx.data as GComponent;
            int childIdx = List.GetChildIndex(obj);
            int dataIdx = List.ChildIndexToItemIndex(childIdx);

            _clickGCompCellHandler?.Invoke(dataIdx, obj);
            _clickCellHandler?.Invoke(dataIdx, obj as T);
        }
    }
}