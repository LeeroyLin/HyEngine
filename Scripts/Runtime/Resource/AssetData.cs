using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
    public class AssetData : MonoBehaviour
    {
        public string relPath;

        private void OnDestroy()
        {
            ResMgr.Ins.ReduceAssetRef(relPath);
        }
    }
}