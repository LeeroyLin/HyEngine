using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
    public class AssetData : MonoBehaviour
    {
        public string relPath;

        public void Dispose()
        {
            ResMgr.Ins.ReduceABRef(relPath);
            
            Object.Destroy(gameObject);
        }
    }
}