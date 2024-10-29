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
            Clear(holder, null);
            
            var obj = PoolMgr.Ins.Get(relPath);
            
            var wrapper = holder.displayObject as GoWrapper;

            if (wrapper == null)
            {
                wrapper = new GoWrapper(obj);
                holder.SetNativeObject(wrapper);
            }
            else
                wrapper.wrapTarget = obj;
            SetTransLayer(obj.transform,LayerMask.NameToLayer("UI"));
            onShow?.Invoke(obj.transform);
        }

        public static void Clear(GGraph holder, Action<GameObject> onRecycle)
        {
            if (holder.displayObject == null)
                return;

            var wrapper = holder.displayObject as GoWrapper;

            if (wrapper == null || wrapper.wrapTarget == null)
                return;

            var obj = wrapper.wrapTarget;
            
            onRecycle?.Invoke(obj);
            
            wrapper.wrapTarget = null;
            
            PoolMgr.Ins.Set(obj);
        }

        public static void SetTransLayer(Transform trans, LayerMask layer)
        {
            trans.gameObject.layer = layer;
            foreach (Transform t in trans)
            {
                SetTransLayer(t, layer);
            }
        }
    }
}