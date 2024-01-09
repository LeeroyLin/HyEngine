using System.Collections.Generic;
using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
    public class ABManifest
    {
        public string version;
        public List<ABManifestFile> files = new List<ABManifestFile>();
        public Dictionary<string, List<string>> dependenceDic = new Dictionary<string, List<string>>();
        public BundleConfig config;
    }

    public class ABManifestFile
    {
        public string fileName;
        public uint crc;
    }
}