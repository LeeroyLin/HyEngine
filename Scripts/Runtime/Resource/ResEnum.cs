namespace Engine.Scripts.Runtime.Resource
{
    public enum EResLoadMode
    {
        // 编辑器直接加载模式
        Editor,
        // ab包模式
        AB,
        // 包内ab包模式
        PackageAB,
        // Resource
        Resource,
    }

    public enum EABState
    {
        SyncLoading,
        AsyncLoading,
        AsyncWaiting,
        Loaded,
        Downloading,
        Downloaded,
    }
    
    public enum EAssetState
    {
        SyncLoading,
        AsyncLoading,
        Loaded,
    }
}