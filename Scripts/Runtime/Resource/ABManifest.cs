using System.Collections.Generic;

namespace Engine.Scripts.Runtime.Resource
{
    public class ABManifest
    {
        public string version;
        
        /// <summary>
        /// 包内的文件
        /// </summary>
        public List<ABManifestFile> packageFiles = new List<ABManifestFile>();
        
        /// <summary>
        /// 提前更新的文件
        /// </summary>
        public List<ABManifestFile> advanceFiles = new List<ABManifestFile>();
        
        /// <summary>
        /// 游戏内更新的文件
        /// </summary>
        public Dictionary<string, ABManifestFile> inGameFiles = new Dictionary<string, ABManifestFile>();
        
        public Dictionary<string, List<string>> dependenceDic = new Dictionary<string, List<string>>();
        public BundleConfig config;
    }

    public class ABManifestFile
    {
        public string fileName;
        public string md5;
    }
}