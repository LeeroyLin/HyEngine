using System.Collections.Generic;

namespace Engine.Scripts.Runtime.Utils.QuadTree
{
    /// <summary>
    /// 每个方向的数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EachSide<T>
    {
        public List<T> datas;
        public QuadTreeNode<T> child;

        public void Init()
        {
        }

        public void Reset()
        {
            if (datas != null)
                datas.Clear();

            child = null;
        }
    }
}