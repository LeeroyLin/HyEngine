using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Scripts.Runtime.Resource
{
    public class AssetInfo
    {
        // 引用计数
        public int RefCnt { get; private set; }

        // 无引用后回调，只有在之前有引用，第一次无引用时才调用
        public Action OnNoRef { get; set; }
        
        // 是否无引用了
        public bool IsNoRef { get; private set; }

        // 资源
        public Object Asset { get; set; }
        
        // AB加载状态
        public EAssetState AssetState { get; set; }

        // 是否加载完毕
        public bool IsLoaded => AssetState == EAssetState.Loaded;

        // 加载完毕回调
        public Action<Object> OnLoaded { get; set; }
        
        // 异步加载对象
        public AssetBundleRequest Req { get; set; }
        
        public bool IsAtlas { get; private set; }
        public string SpriteName { get; set; }
        
        public AssetInfo(EAssetState assetState, bool isAtlas)
        {
            AssetState = assetState;
            IsAtlas = isAtlas;
        }
        
        // 减少引用
        public void ReduceRef(int delta = 1)
        {
            bool isLastNoRef = IsNoRef;
            
            RefCnt -= Mathf.Abs(delta);
            RefCnt = Mathf.Max(0, RefCnt);

            UpdateRefState();
            
            Debug.Log($"CCC ReduceRef RefCnt: {RefCnt}");

            if (!isLastNoRef && IsNoRef)
            {
                OnNoRef?.Invoke();
            }
        }

        // 增加引用
        public void AddRef(int delta = 1)
        {
            RefCnt += delta;
            
            UpdateRefState();
        }

        private void UpdateRefState()
        {
            IsNoRef = RefCnt == 0;
        }
    }
}