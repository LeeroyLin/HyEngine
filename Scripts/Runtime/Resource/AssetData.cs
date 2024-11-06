using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
    public class AssetData : MonoBehaviour
    {
        public string relPath;

        private void OnDestroy()
        {
            if (relPath.StartsWith("Buildings/31"))
            {
                Debug.Log($"CCC OnDestroy {name}");
            }
            
            ResMgr.Ins.ReduceAssetRef(relPath);
        }
    }
}