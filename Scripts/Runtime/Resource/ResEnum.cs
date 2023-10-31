namespace Engine.Scripts.Runtime.Resource
{
    public enum EResLoadMode
    {
        // 编辑器直接加载模式
        Editor,
        // ab包模式
        AB,
    }

    public enum EABState
    {
        SyncLoading,
        AsyncLoading,
        Loaded,
    }
    
    public enum EAssetState
    {
        SyncLoading,
        AsyncLoading,
        Loaded,
    }
}