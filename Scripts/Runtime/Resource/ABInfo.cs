using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Engine.Scripts.Runtime.Resource
{
    public class ABInfo
    { 
        // 引用计数
        public int RefCnt { get; private set; }
        
        /// <summary>
        /// 无引用的时间，Time.time值。有引用后再次无引用会刷新该时间。
        /// </summary>
        public float NoRefAt { get; private set; }

        // 无引用后回调，只有在之前有引用，第一次无引用时才调用
        public Action OnNoRef { get; set; }
        
        // 是否无引用了
        public bool IsNoRef { get; private set; }

        // ab资源
        public AssetBundle AB { get; set; }
        
        // AB加载状态
        public EABState ABState { get; set; }
        
        // 是否依赖加载完毕
        public bool IsDepLoaded { get; set; }

        // 是否加载完毕
        public bool IsLoaded => ABState == EABState.Loaded;

        // 加载完毕回调
        public Action<AssetBundle> OnLoaded { get; set; }
        
        // 异步加载对象
        public AssetBundleCreateRequest Req { get; set; }
        
        // 异步下载对象
        public UnityWebRequestAsyncOperation DownloadReq { get; set; }
        
        public ABInfo(EABState abState)
        {
            ABState = abState;
        }
        
        // 减少引用
        public void ReduceRef(int delta = 1)
        {
            bool isLastNoRef = IsNoRef;
            
            RefCnt -= Mathf.Abs(delta);
            RefCnt = Mathf.Max(0, RefCnt);

            UpdateRefState();

            if (!isLastNoRef && IsNoRef)
            {
                OnNoRef?.Invoke();
                NoRefAt = Time.time;
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