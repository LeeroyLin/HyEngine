using System;
using FairyGUI;

namespace Engine.Scripts.Runtime.View
{
    public class FGUIList<T> where T : GComponent
    {
        public GList List { get; private set; }
        private Action<int, T> _showCellHandler;
        private Action<int, T> _clickCellHandler;
        private Func<int, string> _setCellHandler;

        public FGUIList(GObject obj)
        {
            List = obj as GList;
        }
        
        public void Init(bool isVirtual, int initNum, Action<int, T> showCellHandler, Action<int, T> clickCellHandler = null, Func<int, string> setCellHandler = null)
        {
            if (isVirtual)
                List.SetVirtual();

            _showCellHandler = showCellHandler;

            List.itemRenderer = OnShowListCell;

            if (setCellHandler != null)
            {
                _setCellHandler = setCellHandler;
                List.itemProvider = OnSetListCell;
            }

            if (clickCellHandler != null)
            {
                _clickCellHandler = clickCellHandler;
                List.onClickItem.Add(OnClickCell);
            }
            
            SetNum(initNum);
        }

        public void SetNum(int num)
        {
            List.numItems = num;
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

        void OnShowListCell(int idx, GObject obj)
        {
            _showCellHandler(idx, obj as T);
        }

        string OnSetListCell(int idx)
        {
            return _setCellHandler(idx);
        }

        void OnClickCell(EventContext ctx)
        {
            var obj = ctx.data as T;
            int childIdx = List.GetChildIndex(obj);
            int dataIdx = List.ChildIndexToItemIndex(childIdx);

            _clickCellHandler(dataIdx, obj);
        }
    }
}