using Engine.Scripts.Runtime.ClassPool;

namespace Engine.Scripts.Runtime.Utils.QuadTree
{
    public partial class QuadTree<T>
    {
        void InitClassPool()
        {
            ClassPoolMgr.Ins.InitGroup<QuadTreeNode<T>, QuadTreeNodeInitData>(CreateQuadTreeNode, InitQuadTreeNode, ResetQuadTreeNode, 2000);
            ClassPoolMgr.Ins.InitGroup<EachSide<T>, object>(CreateEachSide, InitEachSide, ResetEachSide, 2000);
        }

        QuadTreeNode<T> CreateQuadTreeNode()
        {
            return new QuadTreeNode<T>();
        }

        void InitQuadTreeNode(QuadTreeNode<T> node, QuadTreeNodeInitData userData)
        {
            node.Init(userData);
        }

        void ResetQuadTreeNode(QuadTreeNode<T> node)
        {
            node.Reset();
        }

        EachSide<T> CreateEachSide()
        {
            return new EachSide<T>();
        }

        void InitEachSide(EachSide<T> node, object userData)
        {
            node.Init();
        }

        void ResetEachSide(EachSide<T> node)
        {
            node.Reset();
        }
    }
}