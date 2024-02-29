using UnityEngine;

namespace Engine.Scripts.Runtime.Utils.QuadTree
{
    public struct QuadTreeNodeInitData
    {
        public Vector2 LTPos;
        public Vector2 RBPos;

        public QuadTreeNodeInitData(Vector2 ltPos, Vector2 rbPos)
        {
            LTPos = ltPos;
            RBPos = rbPos;
        }
    }
}