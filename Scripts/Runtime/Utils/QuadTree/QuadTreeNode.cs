using System.Collections.Generic;
using UnityEngine;

namespace Engine.Scripts.Runtime.Utils.QuadTree
{
    /// <summary>
    /// 四叉树节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QuadTreeNode<T>
    {
        public Dictionary<ESide, EachSide<T>> sideDic;
        public Vector2 centerPos;
        public Vector2 ltPos;
        public Vector2 rbPos;

        public QuadTreeNode()
        {
            sideDic = new Dictionary<ESide, EachSide<T>>();
        }

        public void Init(QuadTreeNodeInitData data)
        {
            ltPos = data.LTPos;
            rbPos = data.RBPos;
            centerPos = (ltPos + rbPos) * .5f;
        }

        public void Reset()
        {
            centerPos = default;
            ltPos = default;
            rbPos = default;
            sideDic.Clear();
        }
    }

}