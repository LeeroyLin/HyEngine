using System;
using Engine.Scripts.Runtime.Resource;
using FairyGUI;
using UnityEngine;

namespace Engine.Scripts.Runtime.View
{
    public class GoWrapperUtil
    {
        public static void Show(GGraph holder, string relPath, Action<Transform> onShow)
        {
            var obj = PoolMgr.Ins.Get(relPath);
            onShow?.Invoke(obj.transform);

            GoWrapper wrapper = new GoWrapper(obj);
            holder.SetNativeObject(wrapper);
        }

        public static void Clear(GGraph holder, Action<GameObject> onRecycle)
        {
            if (holder.displayObject == null)
                return;

            var wrapper = holder.displayObject as GoWrapper;

            if (wrapper == null || wrapper.wrapTarget == null)
                return;
            
            onRecycle?.Invoke(wrapper.wrapTarget);
            
            PoolMgr.Ins.Set(wrapper.wrapTarget);

            wrapper.wrapTarget = null;
        }

        public static void SetTransLayer(Transform trans, LayerMask layer)
        {
            foreach (Transform t in trans)
            {
                t.gameObject.layer = layer;

                SetTransLayer(t, layer);
            }
        }
    }
}